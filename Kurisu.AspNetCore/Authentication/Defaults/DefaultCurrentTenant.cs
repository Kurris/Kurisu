using System;
using Kurisu.Core.User.Abstractions;
using Mapster;
using Microsoft.AspNetCore.Http;

namespace Kurisu.AspNetCore.Authentication.Defaults;

/// <summary>
/// 租户信息获取处理器
/// </summary>
// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
public class DefaultCurrentTenant: ICurrentTenant
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DefaultCurrentTenant(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// 定义租户key
    /// </summary>
    /// <remarks>claims:tenant; header:X-Requested-TenantId</remarks>
    public virtual string TenantKey => "tenant";

    private string _tenantValue = string.Empty;

    public virtual T GetTenantId<T>()
    {
        if (string.IsNullOrEmpty(_tenantValue))
        {
            _tenantValue = _httpContextAccessor?.HttpContext?.User.FindFirst(TenantKey)?.Value;
        }

        return string.IsNullOrEmpty(_tenantValue) ? default : _tenantValue.Adapt<T>();
    }

    public Guid GetUidTenantId() => GetTenantId<Guid>();

    public string GetStringTenantId() => GetTenantId<string>();

    public int GetIntTenantId() => GetTenantId<int>();
}