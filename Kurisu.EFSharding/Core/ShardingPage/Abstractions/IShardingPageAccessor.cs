namespace Kurisu.EFSharding.Core.ShardingPage.Abstractions;

public interface IShardingPageAccessor
{
    ShardingPageContext ShardingPageContext { get; set; }
}