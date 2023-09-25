using System.Reflection;

namespace Kurisu.EFSharding.Sharding.Visitors.Selects;

public class SelectMinProperty:SelectAggregateProperty
{
    public SelectMinProperty(Type ownerType, PropertyInfo property, bool isAggregateMethod, string aggregateMethod) : base(ownerType, property, isAggregateMethod, aggregateMethod)
    {
    }
}