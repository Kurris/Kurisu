using System.Reflection;

namespace AspectCore.DynamicProxy;

[NonAspect]
public sealed class AspectBuilderFactory : IAspectBuilderFactory
{
    private readonly IInterceptorCollector _interceptorCollector;
    private readonly AspectCaching<AspectBuilderFactory, IAspectBuilder> _aspectCaching;

    public AspectBuilderFactory(IInterceptorCollector interceptorCollector,
        AspectCaching<AspectBuilderFactory, IAspectBuilder> aspectCaching)
    {
        _interceptorCollector = interceptorCollector;
        _aspectCaching = aspectCaching;
    }

    public IAspectBuilder Create(AspectContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        return _aspectCaching.GetOrAdd(GetKey(context.ServiceMethod, context.ImplementationMethod), key =>
            Create((Tuple<MethodInfo, MethodInfo>)key)
        );
    }

    private IAspectBuilder Create(Tuple<MethodInfo, MethodInfo> tuple)
    {
        var aspectBuilder = new AspectBuilder(context => context.Complete(), null);

        foreach (var interceptor in _interceptorCollector.Collect(tuple.Item1, tuple.Item2))
            aspectBuilder.AddAspectDelegate(interceptor.Invoke);

        return aspectBuilder;
    }

    private static object GetKey(MethodInfo serviceMethod, MethodInfo implementationMethod)
    {
        return Tuple.Create(serviceMethod, implementationMethod);
    }
}