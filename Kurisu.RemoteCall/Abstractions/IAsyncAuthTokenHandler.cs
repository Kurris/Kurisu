namespace Kurisu.RemoteCall.Abstractions;

/// <summary>
/// 授权token处理
/// </summary>
public interface IAsyncAuthTokenHandler
{
    /// <summary>
    /// 获取token
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <returns></returns>
    Task<string> GetTokenAsync(IServiceProvider serviceProvider);
}
