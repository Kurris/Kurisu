using Kurisu.EFSharding.Sharding.Abstractions;

namespace Kurisu.EFSharding.Core.RuntimeContexts;

public interface IShardingRuntimeContext<TDbContext> : IShardingRuntimeContext where TDbContext : IShardingDbContext
{

}