namespace Kurisu.EFSharding.Core.ShardingPage.Abstractions;

public interface IShardingPageManager
{
    ShardingPageContext Current { get; }

    /// <summary>
    /// 创建分页scope
    /// </summary>
    /// <returns></returns>
    ShardingPageScope CreateScope();
}