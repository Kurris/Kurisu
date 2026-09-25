namespace Kurisu.Extensions.Cache.MethodCaching;

/// <summary>每个容器一个实例；引用计数涵盖持有者和等待者，避免为每个历史 Key 永久保存锁。</summary>
internal sealed class MethodCacheCoordinator
{
    private readonly Dictionary<string, Entry> _entries = new(StringComparer.Ordinal);

    public async Task<IDisposable> EnterAsync(string key, CancellationToken cancellationToken)
    {
        Entry entry;
        lock (_entries)
        {
            if (!_entries.TryGetValue(key, out entry))
                _entries.Add(key, entry = new Entry());
            entry.References++;
        }
        try
        {
            await entry.Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            return new Lease(this, key, entry);
        }
        catch
        {
            Release(key, entry, false);
            throw;
        }
    }

    private void Release(string key, Entry entry, bool acquired)
    {
        lock (_entries)
        {
            if (acquired) entry.Gate.Release();
            if (--entry.References == 0)
            {
                _entries.Remove(key);
                entry.Gate.Dispose();
            }
        }
    }

    private sealed class Entry
    {
        public readonly SemaphoreSlim Gate = new(1, 1);
        public int References;
    }

    private sealed class Lease(MethodCacheCoordinator owner, string key, Entry entry) : IDisposable
    {
        private int _disposed;
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0) owner.Release(key, entry, true);
        }
    }
}
