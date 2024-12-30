using System.Reflection;

namespace Kurisu.Aspect.Core.DynamicProxy;

internal class AspectBuilderFactory
{
    private readonly AspectCaching<Tuple<MethodInfo, MethodInfo>, AspectBuilder> _aspectCaching;
    private readonly InterceptorCollector _interceptorCollector;

    public AspectBuilderFactory(AspectCaching<Tuple<MethodInfo, MethodInfo>, AspectBuilder> aspectCaching, InterceptorCollector interceptorCollector)
    {
        _aspectCaching = aspectCaching;
        _interceptorCollector = interceptorCollector;
    }

    public AspectBuilder Create(MethodInfo serviceMethod, MethodInfo implementationMethod)
    {
        if (serviceMethod == null) throw new ArgumentNullException(nameof(serviceMethod));
        if (implementationMethod == null) throw new ArgumentNullException(nameof(implementationMethod));

        return _aspectCaching.GetOrAdd(Tuple.Create(serviceMethod, implementationMethod), tuple =>
        {
            var aspectBuilder = new AspectBuilder(@delegate => @delegate.Complete());

            foreach (var interceptor in _interceptorCollector.Collect(tuple.Item1, tuple.Item2).ToList())
            {
                aspectBuilder.AddAspectDelegate(interceptor.Invoke);
            }

            return aspectBuilder;
        });
    }
}