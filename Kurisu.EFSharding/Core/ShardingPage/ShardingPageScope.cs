using Kurisu.EFSharding.Core.ShardingPage.Abstractions;

namespace Kurisu.EFSharding.Core.ShardingPage;

public class ShardingPageScope : IDisposable
{

    /// <summary>
    /// 分表配置访问器
    /// </summary>
    public IShardingPageAccessor ShardingPageAccessor { get; }

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="shardingPageAccessor"></param>
    public ShardingPageScope(IShardingPageAccessor shardingPageAccessor)
    {
        ShardingPageAccessor = shardingPageAccessor;
    }

    /// <summary>
    /// 回收
    /// </summary>
    public void Dispose()
    {
        ShardingPageAccessor.ShardingPageContext = null;
    }
}