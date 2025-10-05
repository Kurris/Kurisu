using System.Collections.Concurrent;

namespace AspectCore.DynamicProxy;

[NonAspect]
public class AspectCaching<TBelong, TValue>
{
    private readonly ConcurrentDictionary<object, TValue> _dictionary = new();

    public string Name => typeof(TBelong).Name;

    public TValue Get(object key)
    {
        return _dictionary[key];
    }

    public TValue GetOrAdd(object key, Func<object, TValue> factory)
    {
        return _dictionary.GetOrAdd(key, factory);
    }

    public void Set(object key, TValue value)
    {
        _dictionary[key] = value;
    }
}