﻿using System.Collections;
using Kurisu.EFSharding.Extensions;

namespace Kurisu.EFSharding.Sharding.Enumerators.StreamMergeAsync;

internal class InMemoryStreamMergeAsyncEnumerator<T> : IInMemoryStreamMergeAsyncEnumerator<T>
{
    private readonly bool _async;
    private readonly IEnumerator<T> _inMemoryEnumerator;
    private bool skip;
    private int _inMemoryReallyCount;

    public InMemoryStreamMergeAsyncEnumerator(IStreamMergeAsyncEnumerator<T> asyncSource, bool async)
    {
        if (_inMemoryEnumerator != null)
            throw new ArgumentNullException(nameof(_inMemoryEnumerator));
        _async = async;

        if (_async)
            _inMemoryEnumerator = GetAllRowsAsync(asyncSource).WaitAndUnwrapException();
        else
            _inMemoryEnumerator = GetAllRows(asyncSource);
        _inMemoryEnumerator.MoveNext();
        skip = true;
    }

    private async Task<IEnumerator<T>> GetAllRowsAsync(IStreamMergeAsyncEnumerator<T> streamMergeAsyncEnumerator)
    {
        var list = new List<T>();
#if !EFCORE2
        while (await streamMergeAsyncEnumerator.MoveNextAsync())
#endif
#if EFCORE2
            while (await streamMergeAsyncEnumerator.MoveNext(new CancellationToken()))
#endif
        {
            list.Add(streamMergeAsyncEnumerator.GetCurrent());
            _inMemoryReallyCount++;
        }
        return list.GetEnumerator();
    }
    private IEnumerator<T> GetAllRows(IStreamMergeAsyncEnumerator<T> streamMergeAsyncEnumerator)
    {
        var list = new List<T>();
#if !EFCORE2
        while ( streamMergeAsyncEnumerator.MoveNext())
#endif
#if EFCORE2
            while (streamMergeAsyncEnumerator.MoveNext())
#endif
        {
            list.Add(streamMergeAsyncEnumerator.GetCurrent());
            _inMemoryReallyCount++;
        }

        return list.GetEnumerator();
    }

    public bool SkipFirst()
    {
        if (skip)
        {
            skip = false;
            return true;
        }
        return false;
    }
    public int GetReallyCount()
    {
        return _inMemoryReallyCount;
    }
#if !EFCORE2

    public ValueTask DisposeAsync()
    {
        _inMemoryEnumerator.Dispose();
        return new ValueTask();
    }

    public ValueTask<bool> MoveNextAsync()
    {
        if (skip)
        {
            skip = false;
            return new ValueTask<bool>(null != _inMemoryEnumerator.Current);
        }
        return new ValueTask<bool>(_inMemoryEnumerator.MoveNext());
    }

    public void Dispose()
    {
        _inMemoryEnumerator?.Dispose();
    }

#endif
    public bool MoveNext()
    {
        if (skip)
        {
            skip = false;
            return null != _inMemoryEnumerator.Current;
        }
        return _inMemoryEnumerator.MoveNext();
    }

    public bool HasElement()
    {
        return null != _inMemoryEnumerator.Current;
    }


    public void Reset()
    {
        _inMemoryEnumerator.Reset();
    }

    object IEnumerator.Current => Current;
    public T Current => GetCurrent();
    public T ReallyCurrent => GetReallyCurrent();
    public T GetCurrent()
    {
        if (skip)
            return default;
        return _inMemoryEnumerator.Current;
    }
    public T GetReallyCurrent()
    {
        return _inMemoryEnumerator.Current;
    }
#if EFCORE2
        public void Dispose()
        {
            _inMemoryEnumerator?.Dispose();
        }

        public Task<bool> MoveNext(CancellationToken cancellationToken = new CancellationToken())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (skip)
            {
                skip = false;
                return Task.FromResult(null != _inMemoryEnumerator.Current);
            }
            return Task.FromResult(_inMemoryEnumerator.MoveNext());
        }


#endif
}