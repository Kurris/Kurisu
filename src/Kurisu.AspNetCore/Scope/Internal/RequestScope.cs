using System;
using System.Threading.Tasks;
using Kurisu.AspNetCore.Scope.Abstractions;

namespace Kurisu.AspNetCore.Scope.Internal;

internal class RequestScope : IScope
{
    public void Invoke(Action<IServiceProvider> handler)
    {
        var sp = App.GetServiceProvider();
        handler.Invoke(sp);
    }
    
    public async Task InvokeAsync(Func<IServiceProvider, Task> handler)
    {
        var sp = App.GetServiceProvider();
        await handler.Invoke(sp);
    }

    public TResult Invoke<TResult>(Func<IServiceProvider, TResult> handler)
    {
        var sp = App.GetServiceProvider();
        return handler.Invoke(sp);
    }

    public async Task<TResult> InvokeAsync<TResult>(Func<IServiceProvider, Task<TResult>> handler)
    {
        var sp = App.GetServiceProvider();
        return await handler.Invoke(sp);
    }
}