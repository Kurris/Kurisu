using System.Collections.Immutable;
using System.Reflection;
using Kurisu.RemoteCall.Abstractions;
using Kurisu.RemoteCall.Attributes;
using Kurisu.RemoteCall.Proxy.Abstractions;
using Kurisu.RemoteCall.Utils;
using Microsoft.Extensions.Logging;

namespace Kurisu.RemoteCall;

internal class HttpClientRemoteCallClient : BaseRemoteCallClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IRemoteCallUrlResolver _urlResolver;
    private readonly IRemoteCallParameterValidator _validator;

    private static readonly IReadOnlyDictionary<HttpMethodType, HttpMethod> HttpMethods = new Dictionary<HttpMethodType, HttpMethod>
    {
        [HttpMethodType.Get] = HttpMethod.Get,
        [HttpMethodType.Delete] = HttpMethod.Delete,
        [HttpMethodType.Post] = HttpMethod.Post,
        [HttpMethodType.Put] = HttpMethod.Put,
        [HttpMethodType.Patch] = HttpMethod.Patch
    }.ToImmutableDictionary();

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
        var remoteClient = invocation.InterfaceType.GetCustomAttribute<EnableRemoteClientAttribute>()!;
        var httpMethodAttr = invocation.Method.GetCustomAttribute<HttpMethodAttribute>() ?? throw new NullReferenceException("请定义请求方式");
        var httpClient = string.IsNullOrEmpty(remoteClient.Name)
            ? _httpClientFactory.CreateClient()
            : _httpClientFactory.CreateClient(remoteClient.Name);


        var parameters = invocation.Method.GetParameters();
        var paramValues = parameters.Select((x, idx) => new ParameterValue(x, invocation.Parameters[idx])).ToList();
        _validator.Validate(paramValues);

        var url = _urlResolver.GetUrl(httpMethodAttr.HttpMethod, remoteClient.BaseUrl, httpMethodAttr.Template, paramValues);
        var httpMethod = HttpMethods[httpMethodAttr.HttpMethod];

        HttpContent content = null;

        if (httpMethodAttr.HttpMethod != HttpMethodType.Get)
        {
            var contentHandlerType = invocation.GetCustomAttribute<RequestContentHandlerAttribute>()?.ContentHandler;
            if (contentHandlerType != null)
            {
                var contentHandler = HandlerCache.ContentHandlers.GetOrAdd(contentHandlerType,
                    t => (IRemoteCallContentHandler)Activator.CreateInstance(t));

                var method = contentHandler.GetType().GetTypeInfo().GetRuntimeMethod(invocation.Method.Name, parameters.Select(x => x.ParameterType).ToArray())!;
                content = (HttpContent)method.Invoke(contentHandler, invocation.Parameters);
            }
            else
            {
                content = ContentUtils.Create(invocation.Method, paramValues);
            }
        }

        var (useAuth, headerName, token) = await invocation.UseAuthAsync();
        if (useAuth)
            httpClient.DefaultRequestHeaders.Add(headerName, token);


        var response = await httpClient.SendAsync(new HttpRequestMessage
        {
            Content = content,
            Method = httpMethod,
            RequestUri = new Uri(url)
        });

        var responseJson = await response.Content.ReadAsStringAsync();

        if (invocation.UseLog())
        {
            var body = content != null ? await content.ReadAsStringAsync() : string.Empty;
            Logger.LogInformation("{method} {url}\r\nBody:{body}\r\nResponse:{response} .", httpMethodAttr.HttpMethod, url, body, responseJson);
        }

        var resultHandlerType = invocation.GetCustomAttribute<ResultHandlerAttribute>()?.Handler ?? typeof(void);
        var resultHandler = HandlerCache.ResultHandlers.GetOrAdd(resultHandlerType,
            t => (IRemoteCallResultHandler)Activator.CreateInstance(t));

        return resultHandler.Handle<TResult>(response.StatusCode, responseJson);
    }
}