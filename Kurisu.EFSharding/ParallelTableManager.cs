using Kurisu.EFSharding.Extensions;

namespace Kurisu.EFSharding;

public sealed class ParallelTableManager : IParallelTableManager
{
    private readonly ISet<ParallelTableGroupNode> _parallelTableConfigs = new HashSet<ParallelTableGroupNode>();
    public bool AddParallelTable(ParallelTableGroupNode parallelTableGroupNode)
    {
        return _parallelTableConfigs.Add(parallelTableGroupNode);
    }

    public bool IsParallelTableQuery(IEnumerable<Type> entityTypes)
    {
        if (entityTypes.IsEmpty())
            return false;
        var parallelTableGroupNode = new ParallelTableGroupNode(entityTypes.Select(o=>new ParallelTableComparerType(o)));
        return IsParallelTableQuery(parallelTableGroupNode);
    }

    public bool IsParallelTableQuery(ParallelTableGroupNode parallelTableGroupNode)
    {
        return parallelTableGroupNode != null && _parallelTableConfigs.Contains(parallelTableGroupNode);
    }
}