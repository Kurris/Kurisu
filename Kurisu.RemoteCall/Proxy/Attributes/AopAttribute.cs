using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.RemoteCall.Proxy.Attributes;

/// <summary>
/// Aop代理特性
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Interface)]
public class AopAttribute : Attribute
{
    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="interceptor"></param>
    public AopAttribute(Type interceptor)
    {
        Interceptor = interceptor;
    }

    /// <summary>
    /// 拦截器类型
    /// </summary>
    public Type Interceptor { get; }

    /// <summary>
    /// 配置服务注入
    /// </summary>
    /// <param name="services"></param>
    public virtual void ConfigureServices(IServiceCollection services)
    {
    }
}