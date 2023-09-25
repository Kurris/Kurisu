using System.Collections.Immutable;

namespace Kurisu.EFSharding.Core;

public sealed class SafeReadAppendList<T>
{
    private ImmutableList<T> _list;
    private readonly object _lock = new();
    private List<T> _copyList = new(0);

    public SafeReadAppendList(IEnumerable<T> list)
    {
        _list = ImmutableList.CreateRange(list);
    }

    public SafeReadAppendList() : this(new List<T>(0))
    {
    }

    public List<T> CopyList
    {
        get
        {
            if (_copyList.Count != _list.Count)
            {
                lock (_lock)
                {
                    if (_copyList.Count != _list.Count)
                    {
                        _copyList = _list.ToList();
                    }
                }
            }

            return _copyList;
        }
    }


    public void Append(T value)
    {
        ImmutableList<T> original;
        ImmutableList<T> afterChange;
        do
        {
            original = _list;
            afterChange = _list.Add(value);
        } while (Interlocked.CompareExchange(ref _list, afterChange, original) != original);
    }

    public bool Contains(T value)
    {
        return _list.Contains(value);
    }
}