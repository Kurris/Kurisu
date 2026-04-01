namespace Kurisu.AspNetCore.Abstractions.Utils.Disposables;

public sealed class ActionScope : IDisposable
{
    private Action _action;

    public ActionScope(Action action)
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