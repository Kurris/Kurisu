using System.Reflection;
using Kurisu.Core.Proxy.Abstractions;
using Kurisu.Core.Proxy.Internal;

namespace Kurisu.Core.Proxy;

public class ProxyGenerator : DispatchProxy
{
    /// <summary>
    /// DispatchProxy需要无参构造函数
    /// </summary>
    // ReSharper disable once EmptyConstructor
    public ProxyGenerator()
    {
    }

    protected IInterceptor Interceptor { get; set; }
    protected internal object Target { get; set; }
    protected internal Type InterfaceType { get; set; }

    private static readonly MethodInfo _createMethod = typeof(DispatchProxy).GetMethod(nameof(DispatchProxy.Create), BindingFlags.Static | BindingFlags.Public)!;

    public static T Create<T>(object target, IInterceptor interceptor) where T : class
    {
        return Create(target, typeof(T), interceptor) as T;
    }

    public static object Create(object target, Type interfaceType, IInterceptor interceptor)
    {
        ProxyGenerator proxy = (ProxyGenerator)_createMethod!.MakeGenericMethod(interfaceType, typeof(ProxyGenerator)).Invoke(null, null)!;
        proxy.Target = target;
        proxy.Interceptor = interceptor;
        proxy.InterfaceType = interfaceType;
        return proxy;
    }


    protected override object Invoke(MethodInfo method, object[] args)
    {
        var info = new ProxyInfo()
        {
            Target = Target,
            Method = method,
            Parameters = args,
            InterfaceType = InterfaceType
        };

        if (info.IsInterceptor(Interceptor))
        {
            Interceptor.Intercept(info);
        }
        else
        {
            info.Proceed();
        }

        return info.ReturnValue;
    }
}