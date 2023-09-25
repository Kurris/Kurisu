namespace Kurisu.EFSharding.Extensions.ShardingQueryableExtensions;

public class ShardingQueryableUseConnectionModeOptions
{
    public ShardingQueryableUseConnectionModeOptions(int maxQueryConnectionsLimit)
    {
        MaxQueryConnectionsLimit = maxQueryConnectionsLimit;
    }

    public int MaxQueryConnectionsLimit { get; }
}