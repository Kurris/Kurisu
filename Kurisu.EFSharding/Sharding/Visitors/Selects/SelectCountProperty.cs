using System.Reflection;

namespace Kurisu.EFSharding.Sharding.Visitors.Selects;

public class SelectCountProperty:SelectAggregateProperty
{

    public SelectCountProperty(Type ownerType, PropertyInfo property, bool isAggregateMethod, string aggregateMethod) : base(ownerType, property, isAggregateMethod, aggregateMethod)
    {
    }
}