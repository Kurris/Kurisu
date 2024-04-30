namespace Kurisu.Aspect.Core.DynamicProxy;

public interface IAspectExecutorFactory
{
    IAspectExecutor Create();
}

internal class AspectExecutorFactory : IAspectExecutorFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly AspectBuilderFactory _aspectBuilderFactory;

    public AspectExecutorFactory(IServiceProvider serviceProvider, AspectBuilderFactory aspectBuilderFactory)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _aspectBuilderFactory = aspectBuilderFactory ?? throw new ArgumentNullException(nameof(aspectBuilderFactory));
    }

    public IAspectExecutor Create()
    {
        return new AspectExecutor(_serviceProvider, _aspectBuilderFactory);
    }
}