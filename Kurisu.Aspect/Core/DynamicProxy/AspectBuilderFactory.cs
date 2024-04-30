using System.Reflection;
using Kurisu.Aspect.DynamicProxy;

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

    public AspectBuilder Create(AspectContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        return _aspectCaching.GetOrAdd(Tuple.Create(context.ServiceMethod, context.ImplementationMethod), tuple =>
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