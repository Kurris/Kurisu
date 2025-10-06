using Kurisu.AspNetCore.Abstractions.DependencyInjection;

namespace Kurisu.AspNetCore.Abstractions.Authentication;

/// <summary>
/// 表示当前租户信息的接口，提供获取租户标识和租户ID的方法。
/// </summary>
[SkipScan]
public interface ICurrentTenant
{
    /// <summary>
    /// 获取当前租户的唯一标识。
    /// 来源：Claims中的tenant，或请求头X-Requested-TenantId。
    /// </summary>
    string TenantKey { get; }

    /// <summary>
    /// 获取当前租户的ID。
    /// </summary>
    /// <returns>租户ID，如果不存在则返回null。</returns>
    string GetTenantId();
}