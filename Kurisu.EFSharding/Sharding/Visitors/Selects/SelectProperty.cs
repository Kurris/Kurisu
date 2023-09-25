using System.Reflection;

namespace Kurisu.EFSharding.Sharding.Visitors.Selects;

public class SelectProperty
{
    public SelectProperty( PropertyInfo property)
    {
        Property = property;
    }

    public PropertyInfo Property { get; }
}