using System.Reflection;
using Kurisu.Core.Proxy.Attributes;

namespace Kurisu.Core.Proxy;

public static class ProxyMap
{
    public static readonly Dictionary<Type, List<Type>> InterceptorTypeInvoke = new();
    public static readonly Dictionary<ValueTuple<Type, MethodInfo>, List<Type>> InterceptorMethodInvoke = new();

    public static IEnumerable<Type> GetInterceptorTypes(this Type type)
    {
        var attributes = type.GetCustomAttributes<AopAttribute>();
        var result = attributes.SelectMany(x => x.Interceptors);

        if (result.Any())
        {
            if (!InterceptorTypeInvoke.ContainsKey(type))
            {
                InterceptorTypeInvoke.Add(type, new List<Type>());
            }
            InterceptorTypeInvoke[type].AddRange(result);
        }

        return result;
    }

    public static IEnumerable<Type> GetInterceptorTypes(this MethodInfo[] methods)
    {
        if (methods.Length == 0)
        {
            return Array.Empty<Type>();
        }
        var type = methods[0].DeclaringType;

        var result = methods.SelectMany(method =>
        {
            var r = method.GetCustomAttributes<AopAttribute>()?.SelectMany(x => x.Interceptors) ?? Array.Empty<Type>();
            if (r.Any())
            {
                if (!InterceptorMethodInvoke.ContainsKey((type, method)))
                {
                    InterceptorMethodInvoke.Add((type, method), new List<Type>());
                }
                InterceptorMethodInvoke[(type, method)].AddRange(r);
            }

            return r;
        }).Distinct();


        return result;
    }

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
