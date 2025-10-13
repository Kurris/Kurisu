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

    private static readonly IReadOnlyDictionary<HttpMethodType, MethodInfo> HttpMethods = new Dictionary<HttpMethodType, MethodInfo>
    {
        [HttpMethodType.Get] = typeof(HttpClient).GetMethod("GetAsync", new[] { typeof(string) }),
        [HttpMethodType.Delete] = typeof(HttpClient).GetMethod("DeleteAsync", new[] { typeof(string) }),
        [HttpMethodType.Post] = typeof(HttpClient).GetMethod("PostAsync", new[] { typeof(string), typeof(HttpContent) }),
        [HttpMethodType.Put] = typeof(HttpClient).GetMethod("PutAsync", new[] { typeof(string), typeof(HttpContent) }),
        [HttpMethodType.Patch] = typeof(HttpClient).GetMethod("PatchAsync", new[] { typeof(string), typeof(HttpContent) })
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
        var useLog = invocation.UseLog();
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

        object[] requestParams;
        if (httpMethodAttr.HttpMethod == HttpMethodType.Get || httpMethodAttr.HttpMethod == HttpMethodType.Delete)
        {
            requestParams = new object[] { url };
        }
        else
        {
            var contentHandlerType = invocation.GetCustomAttribute<RequestContentHandlerAttribute>()?.ContentHandler ?? typeof(void);
            var contentHandler = HandlerCache.ContentHandlers.GetOrAdd(contentHandlerType,
                t => (IRemoteCallContentHandler)Activator.CreateInstance(t));
            var content = contentHandler.Create(new ContentInfo { Method = invocation.Method, Values = paramValues });
            requestParams = new object[] { url, content };
        }

        var (useAuth, headerName, token) = await invocation.UseAuthAsync();
        if (useAuth)
            httpClient.DefaultRequestHeaders.Add(headerName, token);

        var response = await ((Task<HttpResponseMessage>)httpMethod.Invoke(httpClient, requestParams)!).ConfigureAwait(false);
        var responseJson = await response.Content.ReadAsStringAsync();

        if (useLog)
        {
            var body = requestParams.Length > 1 ? await ((HttpContent)requestParams[1]).ReadAsStringAsync() : string.Empty;
            Logger.LogInformation("{method} {url}\r\nBody:{body}\r\nResponse:{response} .", httpMethodAttr.HttpMethod, url, body, responseJson);
        }

        var resultHandlerType = invocation.GetCustomAttribute<ResultHandlerAttribute>()?.Handler ?? typeof(void);
        var resultHandler = HandlerCache.ResultHandlers.GetOrAdd(resultHandlerType,
            t => (IRemoteCallResultHandler)Activator.CreateInstance(t));

        return resultHandler.Handle<TResult>(response.StatusCode, responseJson);
    }
}