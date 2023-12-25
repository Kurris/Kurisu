using Kurisu.Core.Proxy.Attributes;
using Kurisu.SqlSugar.Aops;

namespace Kurisu.SqlSugar.Attributes;

/// <summary>
/// 忽略查询条件 TenantId
/// </summary>
public class IgnoreTenantAttribute : AopAttribute
{
    public IgnoreTenantAttribute()
    {
        if (Interceptors?.Any() != true)
        {
            Interceptors = new[] { typeof(IgnoreTenant) };
        }
    }
}