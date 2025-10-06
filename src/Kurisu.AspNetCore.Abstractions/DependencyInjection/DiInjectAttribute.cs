using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.Abstractions.DependencyInjection;

/// <summary>
/// 依赖注入
/// </summary>
[SkipScan]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Parameter, Inherited = false)]
public class DiInjectAttribute : Attribute
{
    /// <summary>
    /// 默认构造函数，无命名
    /// </summary>
    public DiInjectAttribute() : this(null)
    {
    }

    /// <summary>
    /// 指定命名的构造函数
    /// </summary>
    /// <param name="named">服务命名</param>
    public DiInjectAttribute(string named)
    {
        Named = named;
    }

    /// <summary>
    /// 服务命名
    /// </summary>
    public string Named { get; }

    /// <summary>
    /// 服务生命周期，默认为Scoped
    /// </summary>
    public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Scoped;
}