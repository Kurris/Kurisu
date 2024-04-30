using System.Reflection;
using Kurisu.Aspect.Core.Utils;
using Kurisu.Aspect.Reflection.Extensions;

namespace Kurisu.Aspect.Core.DynamicProxy;

internal static class ProxyTypeGenerator
{
    private static readonly ProxyGeneratorUtils _proxyGeneratorUtils = ProxyGeneratorUtils.Instance;

    internal static Type CreateClassProxyType(Type serviceType, Type implementationType)
    {
        if (serviceType == null)
        {
            throw new ArgumentNullException(nameof(serviceType));
        }

        if (!serviceType.GetTypeInfo().IsClass)
        {
            throw new ArgumentException($"Type '{serviceType}' should be class.", nameof(serviceType));
        }

        return _proxyGeneratorUtils.CreateClassProxy(serviceType, implementationType, GetInterfaces(implementationType).ToArray());
    }

    internal static Type CreateInterfaceProxyType(Type serviceType)
    {
        if (serviceType == null)
        {
            throw new ArgumentNullException(nameof(serviceType));
        }

        if (!serviceType.GetTypeInfo().IsInterface)
        {
            throw new ArgumentException($"Type '{serviceType}' should be interface.", nameof(serviceType));
        }

        return _proxyGeneratorUtils.CreateInterfaceProxy(serviceType, GetInterfaces(serviceType, serviceType).ToArray());
    }

    internal static Type CreateInterfaceProxyType(Type serviceType, Type implementationType)
    {
        if (serviceType == null)
        {
            throw new ArgumentNullException(nameof(serviceType));
        }

        if (!serviceType.GetTypeInfo().IsInterface)
        {
            throw new ArgumentException($"Type '{serviceType}' should be interface.", nameof(serviceType));
        }

        return _proxyGeneratorUtils.CreateInterfaceProxy(serviceType, implementationType, GetInterfaces(implementationType, serviceType).ToArray());
    }

    private static IEnumerable<Type> GetInterfaces(Type type, params Type[] exceptInterfaces)
    {
        var hashSet = new HashSet<Type>(exceptInterfaces);
        foreach (var interfaceType in type.GetTypeInfo().GetInterfaces().Distinct())
        {
            if (!interfaceType.GetTypeInfo().IsVisible())
            {
                continue;
            }

            if (hashSet.Contains(interfaceType))
            {
                continue;
            }

            if (interfaceType.GetTypeInfo().ContainsGenericParameters && type.GetTypeInfo().ContainsGenericParameters)
            {
                if (!hashSet.Contains(interfaceType.GetGenericTypeDefinition()))
                    yield return interfaceType;
            }
            else
            {
                yield return interfaceType;
            }
        }
    }
}