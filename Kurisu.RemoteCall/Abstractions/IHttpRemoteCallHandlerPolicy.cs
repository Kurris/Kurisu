using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.RemoteCall.Abstractions;

/// <summary>
/// http请求消息处理策略
/// </summary>
public interface IHttpRemoteCallHandlerPolicy
{
    IHttpClientBuilder ConfigureHttpClient(IHttpClientBuilder builder);
}
