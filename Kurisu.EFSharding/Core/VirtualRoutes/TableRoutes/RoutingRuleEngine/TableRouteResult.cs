using Kurisu.EFSharding.Extensions;

namespace Kurisu.EFSharding.Core.VirtualRoutes.TableRoutes.RoutingRuleEngine;

public class TableRouteResult
{
    public TableRouteResult(IReadOnlyCollection<TableRouteUnit> replaceTables)
    {
        ReplaceTables = replaceTables.ToHashSet();
        HasDifferentTail = ReplaceTables.IsNotEmpty() && ReplaceTables.GroupBy(o => o.Tail).Count() != 1;
        IsEmpty = replaceTables.Count == 0;
    }

    public ISet<TableRouteUnit> ReplaceTables { get; }

    public bool HasDifferentTail { get; }
    public bool IsEmpty { get; }

    protected bool Equals(TableRouteResult other)
    {
        return Equals(ReplaceTables, other.ReplaceTables) && HasDifferentTail == other.HasDifferentTail && IsEmpty == other.IsEmpty;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((TableRouteResult) obj);
    }

    public override string ToString()
    {
        return $"(has different tail:{HasDifferentTail},current table:[{string.Join(",", ReplaceTables.Select(o => $"{o.DataSourceName}.{o.Tail}.{o.EntityType}"))}])";
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ReplaceTables, HasDifferentTail, IsEmpty);
    }
}