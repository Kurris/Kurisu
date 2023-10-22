using System;
using Kurisu.Authentication.Abstractions;
using Mapster;
using Microsoft.AspNetCore.Http;

namespace Kurisu.Authentication.Defaults;

/// <summary>
/// 租户信息获取处理器
/// </summary>
// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
public class DefaultCurrentTenantInfoResolver : ICurrentTenantInfo
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DefaultCurrentTenantInfoResolver(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// 定义租户key
    /// </summary>
    /// <remarks>claims:tenant; header:X-Requested-TenantId</remarks>
    public virtual string TenantKey => "tenant";

    public virtual T GetTenantId<T>()
    {
        var tenantValue = _httpContextAccessor?.HttpContext?.User.FindFirst(TenantKey)?.Value;
        return string.IsNullOrEmpty(tenantValue) ? default : tenantValue.Adapt<T>();
    }

    public Guid GetUidTenantId() => GetTenantId<Guid>();

    public string GetStringTenantId() => GetTenantId<string>();

    public int GetIntTenantId() => GetTenantId<int>();
}