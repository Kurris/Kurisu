using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Core.User.Abstractions;

/// <summary>
/// 当前用户信息
/// </summary>
[SkipScan]
public interface ICurrentUser : ICurrentTenant
{
    /// <summary>
    /// 获取用户id
    /// </summary>
    /// <returns></returns>
    T GetSubjectId<T>();

    /// <summary>
    /// uid类型subject id
    /// </summary>
    /// <returns></returns>
    Guid GetUidSubjectId();

    /// <summary>
    /// string类型subject id
    /// </summary>
    /// <returns></returns>
    string GetStringSubjectId();

    /// <summary>
    /// int类型subject id
    /// </summary>
    /// <returns></returns>
    int GetIntSubjectId();

    /// <summary>
    /// 获取用户名名称
    /// </summary>
    /// <returns></returns>
    string GetName();

    /// <summary>
    /// 获取用户请求access_token
    /// </summary>
    /// <returns></returns>
    string GetToken();

    /// <summary>
    /// 获取用户声明
    /// </summary>
    /// <param name="claimType"></param>
    /// <returns></returns>
    string GetUserClaim(string claimType);
}