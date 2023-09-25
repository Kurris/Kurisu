namespace Kurisu.EFSharding.Core.UnionAllMergeShardingProviders.Abstractions;

public interface IUnionAllMergeAccessor
{
    UnionAllMergeContext SqlSupportContext { get; set; }
}