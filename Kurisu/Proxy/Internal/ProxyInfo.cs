using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Kurisu.Proxy.Abstractions;
using Kurisu.Proxy.Attributes;

namespace Kurisu.Proxy.Internal;

/// <summary>
/// 代理信息
/// </summary>
internal class ProxyInfo : IProxyInvocation
{
    private static readonly ConcurrentDictionary<ValueTuple<Type, MethodInfo>, bool> InterceptorInvoke = new();

    /// <summary>
    /// 接口类型
    /// </summary>
    public Type InterfaceType { get; set; }

    /// <summary>
    /// 代理对象
    /// </summary>
    public object Target { get; set; }

    /// <summary>
    /// 执行方法
    /// </summary>
    public MethodInfo Method { get; set; }

    /// <summary>
    /// 方法参数
    /// </summary>
    public object[] Parameters { get; set; }

    /// <summary>
    /// 方法返回值
    /// </summary>
    public object ReturnValue { get; set; }

    /// <summary>
    /// 执行方法
    /// </summary>
    public void Proceed()
    {
        if (Target != null)
        {
            ReturnValue = Method.Invoke(Target, Parameters);
        }
    }

    internal bool IsInterceptor(IInterceptor interceptor)
    {
        return InterceptorInvoke.GetOrAdd((InterfaceType, Method), _ =>
        {
            if (Target == null)
            {
                return true;
            }

            var type = GetRealTarget(Target).GetType();
            var interceptorType = GetRealType(interceptor);

            var typeExists = type.GetCustomAttributes<AopAttribute>().SelectMany(x => x.Interceptors).Any(x => x.Equals(interceptorType))
                || InterfaceType.GetCustomAttributes<AopAttribute>().SelectMany(x => x.Interceptors).Any(x => x.Equals(interceptorType));

            if (!typeExists)
            {
                var methodExists = Method.GetCustomAttributes<AopAttribute>().SelectMany(x => x.Interceptors).Any(x => x.Equals(interceptorType));
                if (!methodExists)
                {
                    return type.GetMethod(Method.Name, Method.GetParameters().Select(x => x.ParameterType).ToArray()).GetCustomAttributes<AopAttribute>()
                        .SelectMany(x => x.Interceptors).Any(x => x.Equals(interceptorType));
                }
                return methodExists;
            }

            return true;
        });
    }

    /// <summary>
    /// 获取真实的被代理对象
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static object GetRealTarget(object target)
    {
        if (target is not ProxyGenerator o)
        {
            return target;
        }

        return GetRealTarget(o.Target);
    }


    /// <summary>
    /// 真实AOP类型
    /// </summary>
    /// <param name="interceptor"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Type GetRealType(IInterceptor interceptor)
    {
        var t = interceptor.GetType();
        if (typeof(AsyncDeterminationInterceptor).Equals(t))
        {
            return (interceptor as AsyncDeterminationInterceptor).AsyncInterceptor.GetType();
        }
        return t;
    }
}