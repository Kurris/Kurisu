namespace Kurisu.EFSharding.Sharding.MergeContexts;

/// <summary>
/// 优化结果
/// </summary>
internal interface IOptimizeResult
{
    int GetMaxQueryConnectionsLimit();

    bool IsSequenceQuery();
    bool SameWithTailComparer();
    IComparer<string> ShardingTailComparer();
}