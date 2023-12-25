// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 服务注册. 使用Scope生命周期
/// </summary>
[SkipScan]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class ServiceAttribute : Attribute
{
    public ServiceAttribute()
    {
    }

    /// <summary>
    /// 服务注册
    /// </summary>
    /// <param name="named">命名命名</param>
    public ServiceAttribute(string named)
    {
        Named = named;
    }

    /// <summary>
    /// 服务命名
    /// </summary>
    public string Named { get; }

    public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Scoped;
}