using System.Reflection;

namespace Kurisu.EFSharding.Sharding.Visitors.Selects;

public class SelectOwnerProperty : SelectProperty
{
    public SelectOwnerProperty(Type ownerType, PropertyInfo property) : base(property)
    {
        OwnerType = ownerType;
        Property = property;
    }

    public Type OwnerType { get; }
    public PropertyInfo Property { get; }
    public string PropertyName => Property.Name;

    public override string ToString()
    {
        return $"{nameof(OwnerType)}: {OwnerType}, {nameof(Property)}: {Property}, {nameof(PropertyName)}: {PropertyName}";
    }
}