namespace Kurisu.Authentication.Abstractions;

/// <summary>
/// 当前用户信息处理器
/// </summary>
public interface ICurrentUserInfoResolver
{
    /// <summary>
    /// 获取用户id
    /// </summary>
    /// <returns></returns>
    T GetSubjectId<T>();

    /// <summary>
    /// 获取用户请求access_token
    /// </summary>
    /// <returns></returns>
    string GetBearerToken();
}