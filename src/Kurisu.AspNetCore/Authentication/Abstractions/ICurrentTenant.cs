using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.Authentication.Abstractions;

/// <summary>
/// 当前租户信息
/// </summary>
[SkipScan]
public interface ICurrentTenant
{
    /// <summary>
    /// Claims:tenant; header:X-Requested-TenantId
    /// </summary>
    string TenantKey { get; }

    /// <summary>
    /// 获取租户id
    /// </summary>
    /// <returns></returns>
    string GetTenantId();
}