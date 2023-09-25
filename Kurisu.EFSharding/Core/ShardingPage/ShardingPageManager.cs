using Kurisu.EFSharding.Core.ShardingPage.Abstractions;

namespace Kurisu.EFSharding.Core.ShardingPage;

public class ShardingPageManager: IShardingPageManager
{
    private readonly IShardingPageAccessor _shardingPageAccessor;

    public ShardingPageManager(IShardingPageAccessor shardingPageAccessor)
    {
        _shardingPageAccessor = shardingPageAccessor;
    }

    public ShardingPageContext Current => _shardingPageAccessor.ShardingPageContext;
    public ShardingPageScope CreateScope()
    {
        var shardingPageScope = new ShardingPageScope(_shardingPageAccessor);
        _shardingPageAccessor.ShardingPageContext = ShardingPageContext.Create();
        return shardingPageScope;
    }
}