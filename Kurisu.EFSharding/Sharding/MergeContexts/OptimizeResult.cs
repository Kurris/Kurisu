using Kurisu.EFSharding.Core;

namespace Kurisu.EFSharding.Sharding.MergeContexts;

internal sealed class OptimizeResult : IOptimizeResult
{
    private readonly int _maxQueryConnectionsLimit;
    private readonly bool _isSequenceQuery;
    private readonly bool _sameWithTailComparer;
    private readonly IComparer<string> _shardingTailComparer;

    public OptimizeResult(int maxQueryConnectionsLimit, bool isSequenceQuery, bool sameWithTailComparer, IComparer<string> shardingTailComparer)
    {
        _maxQueryConnectionsLimit = maxQueryConnectionsLimit;
        _isSequenceQuery = isSequenceQuery;
        _sameWithTailComparer = sameWithTailComparer;
        _shardingTailComparer = shardingTailComparer;
    }

    public int GetMaxQueryConnectionsLimit()
    {
        return _maxQueryConnectionsLimit;
    }


    public bool IsSequenceQuery()
    {
        return _isSequenceQuery;
    }

    public bool SameWithTailComparer()
    {
        return _sameWithTailComparer;
    }

    public IComparer<string> ShardingTailComparer()
    {
        return _shardingTailComparer;
    }
}