using System.Reflection;

namespace Kurisu.Aspect.DynamicProxy;

/// <summary>
/// AOP代理上下文
/// </summary>
public abstract class AspectContext
{
    public abstract object ReturnValue { get; set; }

    public abstract IServiceProvider ServiceProvider { get; }

    public abstract MethodInfo ServiceMethod { get; }

    public abstract object Implementation { get; }

    public abstract MethodInfo ImplementationMethod { get; }

    public abstract object[] Parameters { get; }

    public abstract MethodInfo ProxyMethod { get; }

    public abstract object Proxy { get; }

    public abstract Task Break();

    public abstract Task Invoke(AspectDelegate next);

    public abstract Task Complete();
}