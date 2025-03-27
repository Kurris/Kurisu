using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.Authentication.Abstractions;

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
    int GetUserId();

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
    /// 获取角色
    /// </summary>
    /// <returns></returns>
    string GetRole();
    
    /// <summary>
    /// 获取用户声明
    /// </summary>
    /// <param name="claimType"></param>
    /// <returns></returns>
    string GetUserClaim(string claimType);
}