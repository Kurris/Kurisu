namespace Kurisu.RemoteCall.Abstractions;

/// <summary>
/// 授权token处理
/// </summary>
public interface IAuthTokenHandler
{
    /// <summary>
    /// 获取token
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <returns></returns>
    string GetToken(IServiceProvider serviceProvider);
}
