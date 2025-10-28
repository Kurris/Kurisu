using System.Reflection;
using Kurisu.RemoteCall.Abstractions;
using Kurisu.RemoteCall.Default;
using Kurisu.RemoteCall.Proxy.Abstractions;
using Kurisu.RemoteCall.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kurisu.RemoteCall.Attributes;

/// <summary>
/// 请求拦截器抽象特性基类
/// </summary>
/// <typeparam name="TResult">远程调用返回结果类型</typeparam>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method)]
public abstract class AbstractRequestInterceptorAttribute<TResult> : Attribute, IRemoteCallInterceptor<TResult>
{
    /// <summary>
    /// 响应体内容（用于日志记录）
    /// </summary>
    private string _responseBody;

    /// <summary>
    /// 请求体内容（用于日志记录）
    /// </summary>
    private string _requestBody;

    /// <summary>
    /// 异常对象 
    /// </summary>
    private Exception _exception;

    /// <summary>
    /// 服务提供器，用于获取依赖服务
    /// </summary>
    public IServiceProvider ServiceProvider { get; set; }

    /// <summary>
    /// 代理调用上下文，包含方法、参数等信息
    /// </summary>
    public IProxyInvocation ProxyInvocation { get; set; }

    /// <summary>
    /// 校验参数合法性，默认调用参数校验器
    /// </summary>
    /// <param name="parameters">方法参数数组</param>
    public virtual void ValidateParameters(object[] parameters)
    {
        var validator = ServiceProvider.GetRequiredService<IRemoteCallParameterValidator>();
        validator.Validate(parameters);
    }

    /// <summary>
    /// 解析请求URL
    /// </summary>
    /// <param name="httpMethod">HTTP方法</param>
    /// <param name="baseUrl">基础URL</param>
    /// <param name="template">路由模板</param>
    /// <param name="wrapParameterValues">参数包装列表</param>
    /// <returns>完整请求URL</returns>
    public virtual string ResolveUrl(string httpMethod, string baseUrl, string template, List<ParameterValue> wrapParameterValues)
    {
        var resolver = ServiceProvider.GetRequiredService<IRemoteCallUrlResolver>();

        return resolver.ResolveUrl(httpMethod,
            baseUrl,
            template,
            wrapParameterValues);
    }

    /// <summary>
    /// 请求前处理，可自定义请求内容（如body），并记录请求体
    /// </summary>
    /// <param name="httpClient">HttpClient实例</param>
    /// <param name="request">HttpRequestMessage实例</param>
    public virtual async Task BeforeRequestAsync(HttpClient httpClient, HttpRequestMessage request)
    {
        await UseAuthAsync(request);
        await UseHeadersAsync(request);

        var jsonSerializer = ServiceProvider.GetRequiredService<IJsonSerializer>();
        var commonUtils = ServiceProvider.GetRequiredService<ICommonUtils>();
        request.Content = HandleHttpContext(request, jsonSerializer, commonUtils);
        _requestBody = request.Content == null ? string.Empty : await request.Content.ReadAsStringAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///  使用请求头，支持自定义头处理器或静态头
    /// </summary>
    /// <param name="request"></param>
    protected virtual async Task UseHeadersAsync(HttpRequestMessage request)
    {
        if (ProxyInvocation.TryGetCustomAttribute<RequestHeaderAttribute>(out var headerAttribute))
        {
            if (headerAttribute.Handler != null)
            {
                var headerHandler = (IRemoteCallHeaderHandler)ServiceProvider.GetRequiredService(headerAttribute.Handler);
                var headers = await headerHandler.GetHeadersAsync().ConfigureAwait(false);
                foreach (var header in headers)
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }
            else
            {
                request.Headers.TryAddWithoutValidation(headerAttribute.Name, headerAttribute.Value);
            }
        }
    }

    /// <summary>
    /// 处理HttpContext，生成请求内容
    /// </summary>
    /// <param name="request">HttpRequestMessage实例</param>
    /// <param name="jsonSerializer">用于序列化对象的 JSON 序列化器适配器</param>
    /// <param name="commonUtils">通用工具实例</param>
    /// <returns>HttpContent实例</returns>
    private HttpContent HandleHttpContext(HttpRequestMessage request, IJsonSerializer jsonSerializer, ICommonUtils commonUtils)
    {
        if ("get".Equals(request.Method.Method, StringComparison.OrdinalIgnoreCase)) return null;

        if (!ProxyInvocation.TryGetCustomAttribute<RequestContentAttribute>(out var contentHandlerAttr))
        {
            return HttpContentUtils.Create(ProxyInvocation.Method, ProxyInvocation.WrapParameterValues, jsonSerializer, commonUtils);
        }

        var contentHandler = (IRemoteCallContentHandler)ServiceProvider.GetRequiredService(contentHandlerAttr.Handler);

        // 缓存方法以提高性能
        var methodKey = (contentHandler.GetType(), ProxyInvocation.Method.Name, string.Join(",", ProxyInvocation.ParameterInfos.Select(p => p.ParameterType.FullName)));
        var method = HandlerCache.Methods.GetOrAdd(methodKey, _ =>
            contentHandler.GetType().GetRuntimeMethod(ProxyInvocation.Method.Name, ProxyInvocation.ParameterInfos.Select(p => p.ParameterType).ToArray())
            ?? throw new InvalidOperationException("请求内容处理器方法未找到"));

        return (HttpContent)method.Invoke(contentHandler, ProxyInvocation.ParameterValues);
    }

    /// <summary>
    /// 认证处理，支持自定义token处理器或从HttpContext获取token
    /// </summary>
    /// <param name="request">HttpRequestMessage实例</param>
    public virtual async Task UseAuthAsync(HttpRequestMessage request)
    {
        if (!ProxyInvocation.TryGetCustomAttribute<RequestAuthorizeAttribute>(out var auth))
        {
            return;
        }

        var headerName = auth.HeaderName;

        var handler = (IRemoteCallAuthTokenHandler)ServiceProvider.GetRequiredService(auth.Handler);
        var token = await handler.GetTokenAsync().ConfigureAwait(false);

        request.Headers.TryAddWithoutValidation(headerName, token);
    }

    /// <summary>
    /// 响应后处理，支持自定义结果处理器，返回最终结果
    /// </summary>
    /// <param name="response">HttpResponseMessage实例</param>
    /// <returns>处理后的结果</returns>
    public virtual async Task<TResult> AfterResponseAsync(HttpResponseMessage response)
    {
        IRemoteCallResponseResultHandler handler;
        if (ProxyInvocation.TryGetCustomAttribute<ResponseResultAttribute>(out var resultHandlerAttr))
        {
            handler = (IRemoteCallResponseResultHandler)ServiceProvider.GetRequiredService(resultHandlerAttr.Handler);
        }
        else
        {
            handler = ServiceProvider.GetRequiredService<DefaultRemoteCallResponseResultHandler>();
        }

        _responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        return handler.Handle<TResult>(response.StatusCode, _responseBody);
    }

    /// <summary>
    /// 异常处理，默认抛出异常，可重写自定义处理
    /// </summary>
    /// <param name="exception">异常对象</param>
    /// <param name="result"></param>
    /// <returns>处理结果</returns>
    public virtual bool TryOnException(Exception exception, out TResult result)
    {
        result = default;
        _exception = exception;

        return false;
    }

    /// <summary>
    /// 日志记录，记录请求方法、URL、请求体和响应体
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="httpMethod">HTTP方法</param>
    /// <param name="url">请求URL</param>
    public virtual void Log(ILogger logger, string httpMethod, string url)
    {
        if (!ProxyInvocation.TryGetCustomAttribute<RequestDisableLogAttribute>(out _))
        {
            if (_exception != null)
            {
                _responseBody = _exception.Message;
            }

            logger.LogInformation("{method} {url}\nBody:{body}\nResponse:{response} .", httpMethod, url, _requestBody, _responseBody);
        }
    }
}