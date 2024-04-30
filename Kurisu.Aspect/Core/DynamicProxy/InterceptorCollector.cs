using System.Reflection;
using Kurisu.Aspect.DynamicProxy;
using Kurisu.Aspect.Reflection;
using Kurisu.Aspect.Reflection.Extensions;

namespace Kurisu.Aspect.Core.DynamicProxy;

internal sealed class InterceptorCollector
{
    private readonly AspectCaching<Tuple<MethodInfo, MethodInfo>, IEnumerable<IInterceptor>> _aspectCaching;

    public InterceptorCollector(AspectCaching<Tuple<MethodInfo, MethodInfo>, IEnumerable<IInterceptor>> aspectCaching)
    {
        _aspectCaching = aspectCaching;
    }

    public IEnumerable<IInterceptor> Collect(MethodInfo serviceMethod, MethodInfo implementationMethod)
    {
        if (serviceMethod == null)
        {
            throw new ArgumentNullException(nameof(serviceMethod));
        }

        if (implementationMethod == null)
        {
            throw new ArgumentNullException(nameof(implementationMethod));
        }

        return _aspectCaching.GetOrAdd(Tuple.Create(serviceMethod, implementationMethod), key => HandleInjector(CollectFromService(serviceMethod)
            .Concat(CollectFromService(implementationMethod))
            .OrderBy(x => x.Order).ToArray())
        );
    }

    private IEnumerable<IInterceptor> CollectFromService(MethodInfo serviceMethod)
    {
        return serviceMethod.GetCustomAttributes<ServiceInterceptorAttribute>().Select(x => (IInterceptor)Activator.CreateInstance(x.InterceptorType, Array.Empty<object>()));
    }

    private IEnumerable<IInterceptor> CollectFromInherited(MethodInfo method)
    {
        var typeInfo = method.DeclaringType.GetTypeInfo();
        var interceptors = new List<IInterceptor>();
        if (!typeInfo.IsClass)
        {
            return interceptors;
        }

        foreach (var interfaceType in typeInfo.GetInterfaces())
        {
            var interfaceMethod = interfaceType.GetTypeInfo().GetDeclaredMethodBySignature(new MethodSignature(method));
            if (interfaceMethod != null)
            {
                interceptors.AddRange(CollectFromService(interfaceMethod));
            }
        }

        interceptors.AddRange(CollectFromBase(method));
        return interceptors;
    }

    private IEnumerable<IInterceptor> CollectFromBase(MethodInfo method)
    {
        var typeInfo = method.DeclaringType.GetTypeInfo();
        var interceptors = new List<IInterceptor>();
        var baseType = typeInfo.BaseType;
        if (baseType == typeof(object) || baseType == null)
        {
            return interceptors;
        }

        var baseMethod = baseType.GetTypeInfo().GetMethodBySignature(new MethodSignature(method));
        if (baseMethod != null)
        {
            interceptors.AddRange(CollectFromBase(baseMethod));
        }

        return interceptors;
    }

    private IEnumerable<IInterceptor> HandleInjector(IEnumerable<IInterceptor> interceptors)
    {
        foreach (var interceptor in interceptors)
        {
            yield return interceptor;
        }
    }
}