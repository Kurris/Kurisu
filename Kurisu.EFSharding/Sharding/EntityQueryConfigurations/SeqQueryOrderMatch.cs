namespace Kurisu.EFSharding.Sharding.EntityQueryConfigurations;

public class SeqQueryOrderMatch
{
    public SeqQueryOrderMatch(bool isSameAsShardingTailComparer, SeqOrderMatchEnum orderMatch)
    {
        IsSameAsShardingTailComparer = isSameAsShardingTailComparer;
        OrderMatch = orderMatch;
    }

    public bool IsSameAsShardingTailComparer { get; }
    public SeqOrderMatchEnum OrderMatch { get; }
}