using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.DataAccess.Sharding.DependencyInjection;

public class ShardingScope : IShardingScope
{
    private readonly IServiceScope _internalServiceScope;
    private readonly IServiceScope _applicationServiceScope;

    public ShardingScope(IServiceScope internalServiceScope, IServiceScope applicationServiceScope)
    {
        _internalServiceScope = internalServiceScope;
        _applicationServiceScope = applicationServiceScope;

        ServiceProvider = new ShardingProvider(internalServiceScope.ServiceProvider, applicationServiceScope?.ServiceProvider);
    }

    /// <summary>
    /// 服务提供器
    /// </summary>
    public IShardingProvider ServiceProvider { get; }

    public void Dispose()
    {
        _internalServiceScope?.Dispose();
        _applicationServiceScope?.Dispose();
    }
}