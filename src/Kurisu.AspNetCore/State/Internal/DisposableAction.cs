using System;

namespace Kurisu.AspNetCore.State.Internal;

internal sealed class DisposableAction : IDisposable
{
    private Action _action;

    public DisposableAction(Action action)
    {
        _action = action;
    }

    public void Dispose()
    {
        var a = _action;
        if (a == null) return;
        _action = null;
        a();
    }
}