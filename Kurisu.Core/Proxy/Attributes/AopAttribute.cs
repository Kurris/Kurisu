using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Core.Proxy.Attributes;

/// <summary>
/// Aop代理特性
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Interface, Inherited = true)]
public class AopAttribute : Attribute
{
    /// <summary>
    /// ctor
    /// </summary>
    public AopAttribute()
    {
    }

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="interceptors"></param>
    public AopAttribute(params Type[] interceptors)
    {
        Interceptors = interceptors;
    }

    /// <summary>
    /// 拦截器类型
    /// </summary>
    public Type[] Interceptors { get; set; }

    /// <summary>
    /// 配置服务注入
    /// </summary>
    /// <param name="services"></param>
    public virtual void ConfigureServices(IServiceCollection services)
    {
    }
}