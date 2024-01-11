using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using Kurisu.Core.Proxy.Abstractions;

namespace Kurisu.Core.Proxy.Internal;

/// <summary>
/// 代理信息
/// </summary>
internal class ProxyInfo : IProxyInvocation
{
    private static readonly ConcurrentDictionary<ValueTuple<Type, ValueTuple<MethodInfo, Type>>, bool> _invoke = new();

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
        var interceptorType = GetRealType(interceptor);
        return _invoke.GetOrAdd((InterfaceType, (Method, interceptorType)), _ =>
        {
            if (Target == null)
            {
                return true;
            }

            var type = GetRealTarget(Target).GetType();

            if (ProxyMap.InterceptorTypeInvoke.TryGetValue(type, out var interceptorTypes))
            {
                if (interceptorTypes.Contains(interceptorType))
                {
                    return true;
                }
            }

            if (ProxyMap.InterceptorTypeInvoke.TryGetValue(InterfaceType, out interceptorTypes))
            {
                if (interceptorTypes.Contains(interceptorType))
                {
                    return true;
                }
            }


            if (ProxyMap.InterceptorMethodInvoke.TryGetValue((type, type.GetMethod(Method.Name, Method.GetParameters().Select(x => x.ParameterType).ToArray())), out interceptorTypes))
            {
                if (interceptorTypes.Contains(interceptorType))
                {
                    return true;
                }
            }

            if (ProxyMap.InterceptorMethodInvoke.TryGetValue((InterfaceType, Method), out interceptorTypes))
            {
                if (interceptorTypes.Contains(interceptorType))
                {
                    return true;
                }
            }

            return false;
        });
    }

    /// <summary>
    /// 获取真实的被代理对象
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static object GetRealTarget(object target)
    {
        if (target is not ProxyGenerator o)
        {
            return target;
        }

        return o.Target != null
            ? GetRealTarget(o.Target)
            : o.InterfaceType;
    }


    /// <summary>
    /// 真实AOP类型
    /// </summary>
    /// <param name="interceptor"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Type GetRealType(IInterceptor interceptor)
    {
        var t = interceptor.GetType();
        return typeof(AsyncDeterminationInterceptor) == t
            ? (interceptor as AsyncDeterminationInterceptor)?.AsyncInterceptor.GetType()
            : t;
    }
}