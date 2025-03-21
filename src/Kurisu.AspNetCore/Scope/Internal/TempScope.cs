using System;
using System.Threading.Tasks;
using Kurisu.AspNetCore.Scope.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.Scope.Internal;

internal class TempScope : IScope
{
    public void Invoke(Action<IServiceProvider> handler)
    {
        using var scope = App.GetServiceProvider(true).CreateScope();
        handler.Invoke(scope.ServiceProvider);
    }

    public async Task InvokeAsync(Func<IServiceProvider, Task> handler)
    {
        await using var scope = App.GetServiceProvider(true).CreateAsyncScope();
        await handler.Invoke(scope.ServiceProvider);
    }

    public TResult Invoke<TResult>(Func<IServiceProvider, TResult> handler)
    {
        using var scope = App.GetServiceProvider(true).CreateScope();
        return handler.Invoke(scope.ServiceProvider);
    }

    public async Task<TResult> InvokeAsync<TResult>(Func<IServiceProvider, Task<TResult>> handler)
    {
        await using var scope = App.GetServiceProvider(true).CreateAsyncScope();
        return await handler.Invoke(scope.ServiceProvider);
    }
}