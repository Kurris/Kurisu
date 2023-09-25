namespace Kurisu.EFSharding.Sharding.MergeContexts;

public class PropertyOrder
{
    public PropertyOrder(string propertyExpression, bool isAsc,Type ownerType)
    {
        PropertyExpression = propertyExpression;
        IsAsc = isAsc;
        OwnerType = ownerType;
    }

    public string PropertyExpression { get; set; }
    public bool IsAsc { get; set; }
    public Type OwnerType { get; }

    public override string ToString()
    {
        return $"{PropertyExpression} {(IsAsc ? "asc" : "desc")}";
    }
}