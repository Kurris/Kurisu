using System.Reflection;
using Kurisu.RemoteCall.Proxy.Attributes;

namespace Kurisu.RemoteCall.Proxy;

/// <summary>
/// 代理类映射
/// </summary>
internal static class ProxyMap
{
    /// <summary>
    /// 类-代理实现
    /// </summary>
    public static readonly Dictionary<Type, List<Type>> InterceptorTypeInvoke = new();

    /// <summary>
    /// 方法-代理实现
    /// </summary>
    public static readonly Dictionary<ValueTuple<Type, MethodInfo>, List<Type>> InterceptorMethodInvoke = new();

    /// <summary>
    /// 获取代理实现
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static IEnumerable<Type> GetInterceptorTypes(this Type type)
    {
        var attributes = type.GetCustomAttributes<AopAttribute>();
        var result = attributes.Select(x => x.Interceptor).ToList();

        if (!result.Any())
        {
            return result;
        }

        if (!InterceptorTypeInvoke.ContainsKey(type))
        {
            InterceptorTypeInvoke.Add(type, new List<Type>());
        }
        InterceptorTypeInvoke[type].AddRange(result);

        return result;
    }

    /// <summary>
    /// 获取代理实现
    /// </summary>
    /// <param name="methods"></param>
    /// <returns></returns>
    public static IEnumerable<Type> GetInterceptorTypes(this MethodInfo[] methods)
    {
        if (methods.Length == 0)
        {
            return Array.Empty<Type>();
        }
        var type = methods[0].DeclaringType;

        var result = methods.SelectMany(method =>
        {
            var r = method.GetCustomAttributes<AopAttribute>().Select(x => x.Interceptor).ToList();
            if (!r.Any())
            {
                return r;
            }

            if (!InterceptorMethodInvoke.ContainsKey((type, method)))
            {
                InterceptorMethodInvoke.Add((type, method), new List<Type>());
            }
            InterceptorMethodInvoke[(type, method)].AddRange(r);

            return r;
        })
        .Distinct();


        return result;
    }

    /// <summary>
    /// 获取代理实现
    /// </summary>
    /// <param name="service"></param>
    /// <param name="interfaceTypes"></param>
    /// <returns></returns>
    public static List<Type> GetAllInterceptorTypes(Type service, IEnumerable<Type> interfaceTypes)
    {
        var interceptorTypes = new List<Type>();

        if (service != null)
        {
            interceptorTypes.AddRange(service.GetInterceptorTypes());
            foreach (var item in service.GetMethods().GetInterceptorTypes())
            {
                if (!interceptorTypes.Contains(item))
                {
                    interceptorTypes.Add(item);
                }
            }
        }

        foreach (var interfaceType in interfaceTypes)
        {
            foreach (var item in interfaceType.GetInterceptorTypes())
            {
                if (!interceptorTypes.Contains(item))
                {
                    interceptorTypes.Add(item);
                }
            }

            foreach (var item in interfaceType.GetMethods().GetInterceptorTypes().Distinct())
            {
                if (!interceptorTypes.Contains(item))
                {
                    interceptorTypes.Add(item);
                }
            }
        }

        return interceptorTypes;
    }
}
