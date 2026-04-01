namespace Kurisu.AspNetCore.Abstractions.Utils.Disposables;

public sealed class AsyncActionScope : IAsyncDisposable
{
    private Func<Task> _action;

    public AsyncActionScope(Func<Task> action)
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