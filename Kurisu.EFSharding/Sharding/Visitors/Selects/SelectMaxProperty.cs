using System.Reflection;

namespace Kurisu.EFSharding.Sharding.Visitors.Selects;

public class SelectMaxProperty:SelectAggregateProperty
{
    public SelectMaxProperty(Type ownerType, PropertyInfo property, bool isAggregateMethod, string aggregateMethod) : base(ownerType, property, isAggregateMethod, aggregateMethod)
    {
    }
}