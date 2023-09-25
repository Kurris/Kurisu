namespace Kurisu.EFSharding.Extensions.ShardingQueryableExtensions;

public class ShardingQueryableAsSequenceOptions
{
    public bool SameWithShardingComparer { get; }
    public bool AsSequence { get; }

    public ShardingQueryableAsSequenceOptions(bool sameWithShardingComparer,bool asSequence)
    {
        SameWithShardingComparer = sameWithShardingComparer;
        AsSequence = asSequence;
    }
}