using System.Reflection;
using Kurisu.RemoteCall.Attributes;
using Kurisu.RemoteCall.Default;
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

    public HttpClientRemoteCallClient(
        IHttpClientFactory httpClientFactory,
        ILogger<HttpClientRemoteCallClient> logger) : base(logger)
    {
        _httpClientFactory = httpClientFactory;
    }

    public override string RequestType => "Http";

    protected override async Task<TResult> RequestAsync<TResult>(IProxyInvocation invocation)
    {
        if (invocation == null) throw new ArgumentNullException(nameof(invocation));

        var client = invocation.RemoteClient;
        var httpMethodDefined = invocation.Method.GetCustomAttribute<BaseHttpMethodAttribute>()
                                ?? throw new NullReferenceException("请定义请求方式");
        var template = httpMethodDefined.Template;
        var httpMethod = httpMethodDefined.HttpMethod.Method;
        var httpClient = string.IsNullOrEmpty(client.Name) ? _httpClientFactory.CreateClient() : _httpClientFactory.CreateClient(client.Name);

        if (!invocation.TryGetCustomAttribute<AbstractRequestInterceptorAttribute<TResult>>(out var interceptor))
        {
            interceptor ??= new DefaultRequestInterceptorAttribute<TResult>();
        }

        interceptor.ProxyInvocation = invocation;
        interceptor.ServiceProvider = invocation.ServiceProvider;

        interceptor.ValidateParameters(invocation.ParameterValues);
        var requestUrl = interceptor.ResolveUrl(httpMethod, client.BaseUrl, template, invocation.WrapParameterValues);

        using var request = new HttpRequestMessage(httpMethodDefined.HttpMethod, new Uri(requestUrl));

        try
        {
            await interceptor.BeforeRequestAsync(httpClient, request).ConfigureAwait(false);
            using var response = await httpClient.SendAsync(request).ConfigureAwait(false);
            return await interceptor.AfterResponseAsync(response).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            if (interceptor.TryOnException(e, out var result))
            {
                return result;
            }

            throw;
        }
        finally
        {
            interceptor.Log(Logger, httpMethod, requestUrl);
        }
    }
}