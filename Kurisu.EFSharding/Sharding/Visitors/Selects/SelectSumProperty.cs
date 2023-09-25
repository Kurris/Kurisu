using System.Reflection;

namespace Kurisu.EFSharding.Sharding.Visitors.Selects;

public class SelectSumProperty:SelectAggregateProperty
{
    public PropertyInfo FromProperty { get; }

    public SelectSumProperty(Type ownerType, PropertyInfo property,PropertyInfo fromProperty, bool isAggregateMethod, string aggregateMethod) : base(ownerType, property, isAggregateMethod, aggregateMethod)
    {
        FromProperty = fromProperty;
    }
}