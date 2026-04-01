using System;

namespace Kurisu.AspNetCore.Abstractions.DataAccess;

public class CompositeDisposableAction : IDisposable
{
    private readonly IDisposable[] _disposables;

    public CompositeDisposableAction(params IDisposable[] disposables)
    {
        _disposables = disposables;
    }

    public void Dispose()
    {
        foreach (var disposable in _disposables)
        {
            disposable?.Dispose();
        }
    }
}
