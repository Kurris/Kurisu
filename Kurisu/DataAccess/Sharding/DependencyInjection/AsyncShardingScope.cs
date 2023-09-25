using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.DataAccess.Sharding.DependencyInjection;

public readonly struct AsyncShardingScope : IShardingScope, IAsyncDisposable
{
    private readonly IServiceScope _internalServiceScope;
    private readonly IServiceScope _applicationServiceScope;

    public AsyncShardingScope(IServiceScope internalServiceScope, IServiceScope applicationServiceScope)
    {
        _internalServiceScope = internalServiceScope;
        _applicationServiceScope = applicationServiceScope;

        ServiceProvider = new ShardingProvider(internalServiceScope.ServiceProvider, _applicationServiceScope?.ServiceProvider);
    }

    public void Dispose()
    {
        _internalServiceScope.Dispose();
        _applicationServiceScope?.Dispose();
    }

    public IShardingProvider ServiceProvider { get; }

    public async ValueTask DisposeAsync()
    {
        if (_internalServiceScope is IAsyncDisposable a)
        {
            await a.DisposeAsync();
        }
        else
        {
            _internalServiceScope.Dispose();
        }

        if (_applicationServiceScope is IAsyncDisposable b)
        {
            await b.DisposeAsync();
        }
        else
        {
            _applicationServiceScope?.Dispose();
        }
    }
}