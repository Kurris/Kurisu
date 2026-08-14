using Kurisu.AspNetCore.Abstractions.Startup;
using Kurisu.Extensions.ContextAccessor.Abstractions;
using Kurisu.Extensions.ContextAccessor.Internal;
using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kurisu.Extensions.ContextAccessor;

public class ContextAccessorBuilder<TState>(IServiceCollection services)
    where TState : class, IContextable<TState>, new()
{
    private static readonly Func<IServiceProvider, object> LifecycleFactory = sp => sp.GetRequiredService<IContextAccessor<TState>>();

    internal ContextAccessorBuilder<TState> WithLifecycle()
    {
        if (services.Any(x =>
                x.ServiceType == typeof(IAppAsyncLocalLifecycle) &&
                x.ImplementationFactory == LifecycleFactory))
        {
            return this;
        }

        services.AddSingleton(typeof(IAppAsyncLocalLifecycle), LifecycleFactory);
        return this;
    }

    public ContextAccessorBuilder<TState> WithSnapshot()
    {
        services.TryAddSingleton(typeof(IContextSnapshotManager<TState>), typeof(ContextSnapshotManager<TState>));
        return this;
    }
}