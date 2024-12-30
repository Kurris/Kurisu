using System.Collections.Concurrent;

namespace Kurisu.Aspect.Core.DynamicProxy;

internal class AspectCaching<TKey, TCache>
{
    private readonly ConcurrentDictionary<TKey, TCache> _dictionary = new();

    public TCache GetOrAdd(TKey key, Func<TKey, TCache> factory)
    {
        return _dictionary.GetOrAdd(key, factory);
    }
}