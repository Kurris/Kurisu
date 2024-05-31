using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Kurisu.RemoteCall.Attributes;
using Kurisu.RemoteCall.Proxy;
using Kurisu.RemoteCall.Proxy.Abstractions;
using Mapster;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Kurisu.RemoteCall.Default;

/// <summary>
/// 默认远程调用
/// </summary>
internal class DefaultRemoteCallClient : Aop
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DefaultRemoteCallClient> _logger;

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="httpClientFactory"></param>
    /// <param name="configuration"></param>
    /// <param name="logger"></param>
    public DefaultRemoteCallClient(
        IServiceProvider serviceProvider,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<DefaultRemoteCallClient> logger)
    {
        _serviceProvider = serviceProvider;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc/>
    protected override async Task InterceptAsync(IProxyInvocation invocation, Func<IProxyInvocation, Task> proceed)
    {
        await RequestAsync(invocation);
    }

    /// <inheritdoc/>
    protected override async Task<TResult> InterceptAsync<TResult>(IProxyInvocation invocation, Func<IProxyInvocation, Task<TResult>> proceed)
    {
        TResult result;

        var responseJson = await RequestAsync(invocation);
        var type = typeof(TResult);

        if (type.IsClass && type != typeof(string))
        {
            result = JsonConvert.DeserializeObject<TResult>(responseJson, InternalHelper.JsonSerializerSettings);
        }
        else
        {
            result = responseJson.Adapt<TResult>();
        }

        return await Task.FromResult(result);
    }

    /// <summary>
    /// 请求处理
    /// </summary>
    /// <param name="invocation"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    /// <exception cref="FileLoadException"></exception>
    private async Task<string> RequestAsync(IProxyInvocation invocation)
    {
        //参数和值
        var methodParameters = invocation.Method.GetParameters();
        var methodParameterValues = methodParameters.Select((t, i) => new KeyValuePair<ParameterInfo, object>(t, invocation.Parameters[i])).ToList();
        //验证
        foreach (var parameter in methodParameterValues)
        {
            if (parameter.Key.ParameterType.IsGenericType && parameter.Key.ParameterType.GetGenericTypeDefinition().IsAssignableTo(typeof(List<>)))
            {
                var a = (IEnumerable<object>)parameter.Value;
                foreach (var item in a)
                {
                    Validator.ValidateObject(item, new ValidationContext(item), true);
                }
            }
            else
            {
                Validator.ValidateObject(parameter.Value, new ValidationContext(parameter.Value), true);
            }
        }

        var defineHttpMethodAttribute = invocation.Method.GetCustomAttribute<HttpMethodAttribute>() ?? throw new NullReferenceException("请定义请求方式");
        var defineRemoteClientAttribute = invocation.InterfaceType.GetCustomAttribute<EnableRemoteClientAttribute>()!;

        (HttpMethodEnumType? httpMethodType, string url) = InternalHelper.GetRequestUrl(_configuration, defineRemoteClientAttribute, defineHttpMethodAttribute, methodParameterValues);

        //请求方法
        var callMethod = (httpMethodType == HttpMethodEnumType.Get
                             ? typeof(HttpClient).GetMethod(httpMethodType + "Async", new[] { typeof(string) })
                             : typeof(HttpClient).GetMethod(httpMethodType + "Async", new[] { typeof(string), typeof(HttpContent) }))
                         ?? throw new NotSupportedException($"不支持{httpMethodType}的请求方式");

        //请求方法的参数
        var requestParameters = new List<object>(2) { url };
        HttpContent content = null;
        if (httpMethodType != HttpMethodEnumType.Get)
        {
            content = await InternalHelper.FixContentAsync(invocation, methodParameterValues);
            requestParameters.Add(content);
        }

        var httpClient = string.IsNullOrEmpty(defineRemoteClientAttribute.Name)
            ? _httpClientFactory.CreateClient()
            : _httpClientFactory.CreateClient(defineRemoteClientAttribute.Name);

        //日志
        if (InternalHelper.UseLog(invocation))
        {
            if (requestParameters.Count > 1)
                _logger.LogInformation("{method} {url} \r\n Body:{body} .", httpMethodType, url, await content!.ReadAsStringAsync());
            else
                _logger.LogInformation("{method} {url} .", httpMethodType, url);
        }

        //鉴权
        if (InternalHelper.UseAuth(_serviceProvider, invocation, out var headerName, out var token))
        {
            httpClient.DefaultRequestHeaders.Add(headerName, token);
        }

        var task = (Task<HttpResponseMessage>)callMethod.Invoke(httpClient, requestParameters.ToArray())!;
        var response = await task.ConfigureAwait(false);

        var responseJson = await response.Content.ReadAsStringAsync();
        if (InternalHelper.UseLog(invocation))
        {
            _logger.LogInformation("Response: {response} .", responseJson);
        }
        
        response.EnsureSuccessStatusCode();
        return responseJson;
    }
}