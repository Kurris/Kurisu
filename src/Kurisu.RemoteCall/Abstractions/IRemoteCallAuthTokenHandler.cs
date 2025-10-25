namespace Kurisu.RemoteCall.Abstractions;

/// <summary>
/// 授权令牌处理器
/// </summary>
public interface IRemoteCallAuthTokenHandler : IRemoteCallHandler
{
    /// <summary>
    /// 获取token
    /// </summary>
    /// <returns></returns>
    Task<string> GetTokenAsync();
}