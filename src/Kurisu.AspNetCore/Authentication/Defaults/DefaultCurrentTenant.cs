using Kurisu.AspNetCore.Authentication.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Kurisu.AspNetCore.Authentication.Defaults;

/// <summary>
/// 租户信息获取处理器
/// </summary>
// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
public class DefaultCurrentTenant : ICurrentTenant
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="httpContextAccessor"></param>
    public DefaultCurrentTenant(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// 定义租户key
    /// </summary>
    /// <remarks>claims:tenant; header:X-Requested-TenantId</remarks>
    public virtual string TenantKey => "tenant";

    /// <summary>
    /// 获取tenant id
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public virtual string GetTenantId()
    {
        return _httpContextAccessor?.HttpContext?.User.FindFirst(TenantKey)?.Value;
    }
}