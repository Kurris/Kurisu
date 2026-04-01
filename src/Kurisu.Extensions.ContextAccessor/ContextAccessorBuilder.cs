using Kurisu.AspNetCore.Abstractions.Startup;
using Kurisu.Extensions.ContextAccessor.Abstractions;
using Kurisu.Extensions.ContextAccessor.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kurisu.Extensions.ContextAccessor;


public class ContextAccessorBuilder<TState> where TState : class, IContextable<TState>, new()
{
    private readonly IServiceCollection _services;

    public ContextAccessorBuilder(IServiceCollection services)
    {
        _services = services;
    }

    internal ContextAccessorBuilder<TState> WithLifecycle()
    {
        _services.AddSingleton(typeof(IAppAsyncLocalLifecycle), sp => sp.GetRequiredService<IContextAccessor<TState>>());
        return this;
    }

    public ContextAccessorBuilder<TState> WithSnapshot()
    {
        _services.TryAddSingleton(typeof(IContextSnapshotManager<TState>), typeof(ContextSnapshotManager<TState>));
        return this;
    }
}
