using System.Reflection;
using Kurisu.RemoteCall.Abstractions;
using Kurisu.RemoteCall.Attributes;
using Kurisu.RemoteCall.Proxy.Abstractions;
using Kurisu.RemoteCall.Utils;
using Microsoft.Extensions.Logging;

namespace Kurisu.RemoteCall;

/// <summary>
/// http客户端远程调用客户端
/// </summary>
internal class HttpClientRemoteCallClient : BaseRemoteCallClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IRemoteCallUrlResolver _urlResolver;
    private readonly IRemoteCallParameterValidator _validator;

    public HttpClientRemoteCallClient(
        IHttpClientFactory httpClientFactory,
        IRemoteCallUrlResolver urlResolver,
        IRemoteCallParameterValidator validator,
        ILogger<HttpClientRemoteCallClient> logger) : base(logger)
    {
        _httpClientFactory = httpClientFactory;
        _urlResolver = urlResolver;
        _validator = validator;
    }

    public override string RequestType => "Http";

    protected override async Task<TResult> RequestAsync<TResult>(IProxyInvocation invocation)
    {
        var client = invocation.RemoteClient;
        var httpMethodDefined = invocation.Method.GetCustomAttribute<BaseHttpMethodAttribute>()
                                ?? throw new NullReferenceException("请定义请求方式");
        var httpClient = string.IsNullOrEmpty(client.Name)
            ? _httpClientFactory.CreateClient()
            : _httpClientFactory.CreateClient(client.Name);

        _validator.Validate(invocation.ParameterValues);

        var url = _urlResolver.ResolveUrl(httpMethodDefined.HttpMethodType, client.BaseUrl, httpMethodDefined.Template, invocation.WrapParameterValues);

        // ReSharper disable once ConvertToUsingDeclaration
        using (var request = new HttpRequestMessage(httpMethodDefined.HttpMethod, new Uri(url)))
        {
            request.Content = CreateContentWhenNotGet(invocation, httpMethodDefined);
            var body = request.Content == null ? string.Empty : await request.Content.ReadAsStringAsync().ConfigureAwait(false);
            await invocation.UseAuthAsync(request).ConfigureAwait(false);

            using (var response = await httpClient.SendAsync(request).ConfigureAwait(false))
            {
                var responseJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (invocation.UseLog())
                {
                    Logger.LogInformation("{method} {url}\r\nBody:{body}\r\nResponse:{response} .", httpMethodDefined.HttpMethodType, url, body, responseJson);
                }

                var resultHandlerType = invocation.GetCustomAttribute<ResultHandlerAttribute>()?.Handler ?? typeof(void);
                var resultHandler = HandlerCache.ResultHandlers.GetOrAdd(resultHandlerType,
                    t => (IRemoteCallResultHandler)Activator.CreateInstance(t));

                return resultHandler.Handle<TResult>(response.StatusCode, responseJson);
            }
        }
    }


    private static HttpContent CreateContentWhenNotGet(IProxyInvocation invocation, BaseHttpMethodAttribute httpMethodDefined)
    {
        if (httpMethodDefined.HttpMethodType == HttpMethodType.Get) return null;

        var contentHandlerType = invocation.GetCustomAttribute<RequestContentHandlerAttribute>()?.Handler;
        if (contentHandlerType == null) return HttpContentUtils.Create(invocation.Method, invocation.WrapParameterValues);

        var contentHandler = HandlerCache.ContentHandlers.GetOrAdd(contentHandlerType,
            t => (IRemoteCallContentHandler)Activator.CreateInstance(t));

        var method = contentHandler.GetType().GetTypeInfo()
                         .GetRuntimeMethod(invocation.Method.Name, invocation.ParameterInfos.Select(p => p.ParameterType).ToArray())
                     ?? throw new NullReferenceException("请求内容处理器方法未找到");

        return (HttpContent)method.Invoke(contentHandler, invocation.ParameterValues);
    }
}