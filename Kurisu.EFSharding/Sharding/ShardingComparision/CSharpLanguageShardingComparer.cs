using System.Collections.Concurrent;
using System.Data.SqlTypes;
using Kurisu.EFSharding.Sharding.Internals;
using Kurisu.EFSharding.Sharding.ShardingComparision.Abstractions;
using Kurisu.EFSharding.Extensions;

namespace Kurisu.EFSharding.Sharding.ShardingComparision;

public class CSharpLanguageShardingComparer : IShardingComparer
{
    private readonly ConcurrentDictionary<Type, object> _comparers = new ();
    public virtual int Compare(IComparable x, IComparable y, bool asc)
    {
        if (x is Guid xg && y is Guid yg)
        {
            return new SqlGuid(xg).SafeCompareToWith(new SqlGuid(yg), asc);
        }
        return x.SafeCompareToWith(y, asc);
    }

    public object CreateComparer(Type comparerType)
    {
        var comparer = _comparers.GetOrAdd(comparerType,
            key => Activator.CreateInstance(typeof(InMemoryShardingComparer<>).GetGenericType0(comparerType),
                this));
        return comparer;
    }
}