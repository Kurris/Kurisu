using Kurisu.AspNetCore.Authentication.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Kurisu.AspNetCore.Authentication.Defaults;

/// <summary>
/// 租户信息获取处理器
/// </summary>
// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
public class DefaultCurrentTenant : ICurrentTenant
{
    /// <summary>
    /// HttpContextAccessor
    /// </summary>
    protected IHttpContextAccessor HttpContextAccessor { get; }

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="httpContextAccessor"></param>
    public DefaultCurrentTenant(IHttpContextAccessor httpContextAccessor)
    {
        HttpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// 定义租户key
    /// </summary>
    /// <remarks>claims:tenant; header:X-Requested-TenantId</remarks>
    public virtual string TenantKey => "tenant";

    /// <summary>
    /// 获取tenant id
    /// </summary>
    /// <returns></returns>
    public virtual string GetTenantId()
    {
        return HttpContextAccessor.HttpContext?.User.FindFirst(TenantKey)?.Value;
    }
}