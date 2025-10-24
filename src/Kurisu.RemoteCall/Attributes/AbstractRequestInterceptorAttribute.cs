using System.Reflection;
using Kurisu.RemoteCall.Abstractions;
using Kurisu.RemoteCall.Default;
using Kurisu.RemoteCall.Proxy.Abstractions;
using Kurisu.RemoteCall.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Kurisu.RemoteCall.Attributes;

/// <summary>
/// 请求拦截器抽象特性基类，定义远程调用的拦截处理流程（如参数校验、URL解析、请求前后处理、异常处理、日志等）。
/// 需继承实现具体业务逻辑。
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
        request.Content = HandleHttpContext();
        _requestBody = request.Content == null ? string.Empty : await request.Content.ReadAsStringAsync().ConfigureAwait(false);
        return;

        // 处理HttpContext，生成请求内容
        HttpContent HandleHttpContext()
        {
            if ("get".Equals(request.Method.Method, StringComparison.OrdinalIgnoreCase)) return null;

            if (!ProxyInvocation.TryGetCustomAttribute<RequestContentHandlerAttribute>(out var contentHandlerAttr))
            {
                return HttpContentUtils.Create(ProxyInvocation.Method, ProxyInvocation.WrapParameterValues);
            }


            var contentHandler = HandlerCache.ContentHandlers.GetOrAdd(contentHandlerAttr.Handler, t => (IRemoteCallContentHandler)Activator.CreateInstance(t));
            var method = contentHandler.GetType().GetTypeInfo()
                             .GetRuntimeMethod(ProxyInvocation.Method.Name, ProxyInvocation.ParameterInfos.Select(p => p.ParameterType).ToArray())
                         ?? throw new NullReferenceException("请求内容处理器方法未找到");

            return (HttpContent)method.Invoke(contentHandler, ProxyInvocation.ParameterValues);
        }
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
        string token;
        if (auth.Handler.IsInheritedFrom<IRemoteCallAuthTokenHandler>())
        {
            var handler = (IRemoteCallAuthTokenHandler)Activator.CreateInstance(auth.Handler)
                          ?? throw new NullReferenceException($"无法创建 {auth.Handler.FullName} 的实例。");

            token = await handler.GetTokenAsync(ServiceProvider).ConfigureAwait(false);
        }
        else
        {
            var httpContext = ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext
                              ?? throw new NullReferenceException("获取HttpContext失败,HttpContext为Null,请检查是否注入IHttpContextAccessor或在请求的作用域中.");

            token = httpContext.Request.Headers[headerName].FirstOrDefault();
            if (string.IsNullOrEmpty(token))
            {
                throw new NullReferenceException($"使用请求的token失败,但HttpContext.Headers[{headerName}] 为Null");
            }
        }

        request.Headers.TryAddWithoutValidation(headerName, token);
    }

    /// <summary>
    /// 响应后处理，支持自定义结果处理器，返回最终结果
    /// </summary>
    /// <param name="response">HttpResponseMessage实例</param>
    /// <returns>处理后的结果</returns>
    public virtual async Task<TResult> AfterResponseAsync(HttpResponseMessage response)
    {
        if (ProxyInvocation.TryGetCustomAttribute<ResultHandlerAttribute>(out var resultHandlerAttr))
        {
            if (!resultHandlerAttr.Handler.IsInheritedFrom<IRemoteCallResultHandler>())
            {
                throw new InvalidOperationException($"请求结果处理器 {resultHandlerAttr.Handler.FullName} 必须实现 IRemoteCallResultHandler 接口");
            }
        }

        var resultHandler = HandlerCache.ResultHandlers.GetOrAdd(resultHandlerAttr?.Handler ?? typeof(RemoteCallStandardResultHandler),
            t => (IRemoteCallResultHandler)Activator.CreateInstance(t)
        );

        _responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        return resultHandler.Handle<TResult>(response.StatusCode, _responseBody);
    }

    /// <summary>
    /// 异常处理，默认抛出异常，可重写自定义处理
    /// </summary>
    /// <param name="exception">异常对象</param>
    /// <returns>处理结果</returns>
    public virtual Task<TResult> OnExceptionAsync(Exception exception)
    {
        throw exception;
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
            logger.LogInformation("{method} {url}\r\nBody:{body}\r\nResponse:{response} .", httpMethod, url, _requestBody, _responseBody);
        }
    }
}