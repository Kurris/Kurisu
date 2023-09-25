using Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines.Abstractions;

namespace Kurisu.EFSharding.Sharding.MergeEngines.Executors.ShardingMergers;

internal class AnyMethodShardingMerger:IShardingMerger<bool>
{
    private static readonly IShardingMerger<bool> _allShardingMerger;

    static AnyMethodShardingMerger()
    {
        _allShardingMerger = new AnyMethodShardingMerger();
    }

    public static IShardingMerger<bool> Instance => _allShardingMerger;

    public bool StreamMerge(List<bool> parallelResults)
    {
        return parallelResults.Any(o => o);
    }

    public void InMemoryMerge(List<bool> beforeInMemoryResults, List<bool> parallelResults)
    {
        beforeInMemoryResults.AddRange(parallelResults);
    }
}