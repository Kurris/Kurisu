using System.Reflection;
using Kurisu.Aspect.Core.Utils;
using Kurisu.Aspect.Reflection.Extensions;

namespace Kurisu.Aspect.Core.DynamicProxy;

public interface IProxyTypeGenerator
{
    Type CreateClassProxyType(Type serviceType, Type implementationType);

    Type CreateInterfaceProxyType(Type serviceType);

    Type CreateInterfaceProxyType(Type serviceType, Type implementationType);
}

internal class ProxyTypeGenerator : IProxyTypeGenerator
{
    private static readonly ProxyGeneratorUtils _proxyGeneratorUtils = ProxyGeneratorUtils.Instance;

    public Type CreateClassProxyType(Type serviceType, Type implementationType)
    {
        if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));
        if (!serviceType.GetTypeInfo().IsClass) throw new ArgumentException($"Type '{serviceType}' should be class.", nameof(serviceType));

        return _proxyGeneratorUtils.CreateClassProxy(serviceType, implementationType, GetInterfaces(implementationType).ToArray());
    }

    public Type CreateInterfaceProxyType(Type serviceType)
    {
        if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));
        if (!serviceType.GetTypeInfo().IsInterface) throw new ArgumentException($"Type '{serviceType}' should be interface.", nameof(serviceType));

        return _proxyGeneratorUtils.CreateInterfaceProxy(serviceType, GetInterfaces(serviceType, serviceType).ToArray());
    }

    public Type CreateInterfaceProxyType(Type serviceType, Type implementationType)
    {
        if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));
        if (!serviceType.GetTypeInfo().IsInterface) throw new ArgumentException($"Type '{serviceType}' should be interface.", nameof(serviceType));

        return _proxyGeneratorUtils.CreateInterfaceProxy(serviceType, implementationType, GetInterfaces(implementationType, serviceType).ToArray());
    }

    private static IEnumerable<Type> GetInterfaces(Type type, params Type[] exceptInterfaces)
    {
        var hashSet = new HashSet<Type>(exceptInterfaces);
        foreach (var interfaceType in type.GetTypeInfo().GetInterfaces().Distinct())
        {
            if (!interfaceType.GetTypeInfo().IsVisible())
                continue;

            if (hashSet.Contains(interfaceType))
                continue;

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