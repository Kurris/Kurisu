using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.RemoteCall.Abstractions;

/// <summary>
/// http请求消息处理策略
/// </summary>
public interface IHttpRemoteCallPolicyHandler
{
    /// <summary>
    /// 配置httpClient
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    IHttpClientBuilder ConfigureHttpClient(IHttpClientBuilder builder);
}
