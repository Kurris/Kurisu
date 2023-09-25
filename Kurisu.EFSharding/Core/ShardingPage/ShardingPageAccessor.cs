using Kurisu.EFSharding.Core.ShardingPage.Abstractions;

namespace Kurisu.EFSharding.Core.ShardingPage;

public class ShardingPageAccessor : IShardingPageAccessor
{
    private static AsyncLocal<ShardingPageContext> _shardingPageContext = new();


    public ShardingPageContext ShardingPageContext
    {
        get => _shardingPageContext.Value;
        set => _shardingPageContext.Value = value;
    }
}