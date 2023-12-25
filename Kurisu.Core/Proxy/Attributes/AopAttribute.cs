using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Core.Proxy.Attributes;

/// <summary>
/// Aop代理特性
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Interface, Inherited = true)]
public class AopAttribute : Attribute
{
    public AopAttribute()
    {
    }

    public AopAttribute(params Type[] interceptors)
    {
        Interceptors = interceptors;
    }

    public Type[] Interceptors { get; set; }


    public virtual void ConfigureServices(IServiceCollection services)
    {
    }
}