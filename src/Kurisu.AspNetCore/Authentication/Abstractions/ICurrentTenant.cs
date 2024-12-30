using System;
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
    T GetTenantId<T>();

    /// <summary>
    /// uid类型
    /// </summary>
    /// <returns></returns>
    Guid GetUidTenantId();

    /// <summary>
    /// string类型
    /// </summary>
    /// <returns></returns>
    string GetStringTenantId();

    /// <summary>
    /// int类型
    /// </summary>
    /// <returns></returns>
    int GetIntTenantId();
}