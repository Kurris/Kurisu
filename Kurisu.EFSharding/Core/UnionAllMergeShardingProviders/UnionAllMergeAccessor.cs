using Kurisu.EFSharding.Core.UnionAllMergeShardingProviders.Abstractions;

namespace Kurisu.EFSharding.Core.UnionAllMergeShardingProviders;

public class UnionAllMergeAccessor : IUnionAllMergeAccessor
{
    private static AsyncLocal<UnionAllMergeContext> _sqlSupportContext = new();


    public UnionAllMergeContext SqlSupportContext
    {
        get => _sqlSupportContext.Value;
        set => _sqlSupportContext.Value = value;
    }
}