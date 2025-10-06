using Kurisu.AspNetCore.Abstractions.DependencyInjection;

namespace Kurisu.AspNetCore.Abstractions.Authentication;

/// <summary>
/// 当前用户信息接口，继承自 <see cref="ICurrentTenant"/>，用于获取当前用户的相关信息。
/// </summary>
[SkipScan]
public interface ICurrentUser : ICurrentTenant
{
    /// <summary>
    /// 获取当前用户的唯一标识（用户ID）。
    /// </summary>
    int GetUserId();

    /// <summary>
    /// 获取当前用户的唯一标识（用户ID）。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    T GetUserId<T>();

    /// <summary>
    /// 获取当前用户的用户名。
    /// </summary>
    /// <param name="userClaimType"></param>
    /// <returns>用户名字符串。</returns>
    string GetName(string userClaimType = "name");

    /// <summary>
    /// 获取当前用户请求时携带的 access_token。
    /// </summary>
    /// <returns>access_token 字符串。</returns>
    string GetAccessToken();

    /// <summary>
    /// 获取当前用户的所有角色信息。
    /// </summary>
    /// <returns>所有角色集合，如无则为空集合。</returns>
    IEnumerable<string> GetRoles();

    /// <summary>
    /// 根据声明类型获取当前用户的声明值。
    /// </summary>
    /// <param name="claimType">声明类型。</param>
    /// <returns>声明值字符串。</returns>
    string GetUserClaim(string claimType);
}