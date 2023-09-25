using Kurisu.EFSharding.Sharding.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Kurisu.EFSharding.EFCores;

/// <summary>
/// 分片model缓存key创建factory
/// </summary>
/// <remarks>
/// 触发DbContext Model重新builder的关键
/// </remarks>
internal class ShardingModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context)
    {
        return Create(context, false);
    }

    public object Create(DbContext context, bool designTime)
    {
        if (context is IShardingDbContext shardingTableDbContext && !string.IsNullOrWhiteSpace(shardingTableDbContext.RouteTail.GetRouteTailIdentity()))
        {
            return $"{context.GetType()}_{shardingTableDbContext.RouteTail.GetRouteTailIdentity()}_{designTime}";
        }

        return (context.GetType(), designTime);
    }
}