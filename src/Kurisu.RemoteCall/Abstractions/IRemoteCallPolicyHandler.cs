using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.RemoteCall.Abstractions;

/// <summary>
/// 请求策略处理器
/// </summary>
public interface IRemoteCallPolicyHandler : IRemoteCallHandler
{
    /// <summary>
    /// 配置httpClient
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    IHttpClientBuilder ConfigureHttpClient(IHttpClientBuilder builder);
}
