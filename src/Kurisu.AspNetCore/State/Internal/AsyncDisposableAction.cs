using System;
using System.Threading.Tasks;

namespace Kurisu.AspNetCore.State.Internal;

internal sealed class AsyncDisposableAction : IAsyncDisposable
{
    private Func<Task> _action;

    public AsyncDisposableAction(Func<Task> action)
    {
        _action = action;
    }

    public async ValueTask DisposeAsync()
    {
        var a = _action;
        if (a == null) return;
        _action = null;
        await a().ConfigureAwait(false);
    }
}