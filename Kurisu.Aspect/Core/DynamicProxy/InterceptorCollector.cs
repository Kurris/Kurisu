using System.Reflection;
using Kurisu.Aspect.DynamicProxy;

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

        return _aspectCaching.GetOrAdd(Tuple.Create(serviceMethod, implementationMethod), key => CollectFromService(key.Item1)
            .Concat(CollectFromService(key.Item2))
            .OrderBy(x => x.Order).ToArray()
        );
    }

    private static IEnumerable<IInterceptor> CollectFromService(MethodInfo serviceMethod)
    {
        return serviceMethod.GetCustomAttributes<ServiceInterceptorAttribute>().Select(x => (IInterceptor)Activator.CreateInstance(x.InterceptorType, Array.Empty<object>()));
    }
}