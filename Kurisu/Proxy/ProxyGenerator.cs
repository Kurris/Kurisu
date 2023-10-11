using System;
using System.Reflection;
using Kurisu.Proxy.Abstractions;
using Kurisu.Proxy.Internal;

namespace Kurisu.Proxy;

public class ProxyGenerator : DispatchProxy
{
    public ProxyGenerator()
    {
    }

    private IInterceptor Interceptor { get; set; }
    internal object Target { get; set; }
    private Type InterfaceType { get; set; }



    private static readonly MethodInfo CreateMethod = typeof(DispatchProxy).GetMethod(nameof(DispatchProxy.Create), BindingFlags.Static | BindingFlags.Public);

    public static T Create<T>(object target, IInterceptor interceptor) where T : class
    {
        return Create(target, typeof(T), interceptor) as T;
    }

    public static object Create(object target, Type interfaceType, IInterceptor interceptor)
    {
        ProxyGenerator proxy = (ProxyGenerator)CreateMethod.MakeGenericMethod(interfaceType, typeof(ProxyGenerator)).Invoke(null, null);
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

