using Kurisu.EFSharding.Sharding.Abstractions;
using Kurisu.EFSharding.Sharding.ReadWriteConfigurations;
using Kurisu.EFSharding.Sharding.ReadWriteConfigurations.Abstractions;
using Kurisu.EFSharding.Extensions.DbContextExtensions;
using Microsoft.EntityFrameworkCore;

namespace Kurisu.EFSharding.Extensions;

public static class ShardingReadWriteExtension
{
    /// <summary>
    /// 设置读写分离读取写数据库
    /// </summary>
    /// <param name="shardingDbContext"></param>
    /// <returns></returns>
    public static bool ReadWriteSeparationWriteOnly(this IShardingDbContext shardingDbContext)
    {
        shardingDbContext.SetReadWriteSeparation(false);
        return true;
    }

    /// <summary>
    /// 设置读写分离读读数据库
    /// </summary>
    /// <param name="shardingDbContext"></param>
    /// <returns></returns>
    public static bool ReadWriteSeparationReadOnly(this IShardingDbContext shardingDbContext)
    {
        shardingDbContext.SetReadWriteSeparation(true);
        return true;
    }

    /// <summary>
    /// 设置读写分离
    /// </summary>
    /// <param name="shardingDbContext"></param>
    /// <param name="readOnly">是否是读数据源</param>
    private static void SetReadWriteSeparation(this IShardingDbContext shardingDbContext, bool readOnly)
    {
        var shardingRuntimeContext = ((DbContext) shardingDbContext).GetShardingRuntimeContext();
        var shardingDbContextExecutor = shardingDbContext.GetShardingExecutor();
        var shardingReadWriteManager = shardingRuntimeContext.GetService<IShardingReadWriteManager>();
        var shardingReadWriteContext = shardingReadWriteManager.GetCurrent();
        if (shardingReadWriteContext != null)
        {
            if (shardingReadWriteContext.DefaultPriority > shardingDbContextExecutor.ReadWriteSeparationPriority)
            {
                shardingDbContextExecutor.ReadWriteSeparationPriority = shardingReadWriteContext.DefaultPriority + 1;
            }
        }

        shardingDbContextExecutor.ReadWriteSeparationBehavior = readOnly ? ReadWriteDefaultEnableBehavior.DefaultEnable : ReadWriteDefaultEnableBehavior.DefaultDisable;
    }

    public static void SetReadWriteSeparation(this ShardingReadWriteContext shardingReadWriteContext, int priority,
        bool readOnly)
    {
        shardingReadWriteContext.DefaultPriority = priority;
        shardingReadWriteContext.DefaultReadEnable = readOnly;
    }


    public static ReadWriteDefaultEnableBehavior CurrentIsReadWriteSeparationBehavior(this IShardingDbContext shardingDbContext)
    {
        if (shardingDbContext.IsUseReadWriteSeparation())
        {
            var shardingRuntimeContext = ((DbContext) shardingDbContext).GetShardingRuntimeContext();
            var shardingDbContextExecutor = shardingDbContext.GetShardingExecutor();
            var shardingReadWriteManager = shardingRuntimeContext.GetService<IShardingReadWriteManager>();
            var shardingReadWriteContext = shardingReadWriteManager.GetCurrent();
            if (shardingReadWriteContext != null)
            {
                if (shardingReadWriteContext.DefaultPriority > shardingDbContextExecutor.ReadWriteSeparationPriority)
                {
                    return shardingReadWriteContext.DefaultEnableBehavior;
                }
                else
                {
                    return shardingDbContextExecutor.ReadWriteSeparationBehavior;
                }
            }

            return shardingDbContextExecutor.ReadWriteSeparationBehavior;
        }

        return ReadWriteDefaultEnableBehavior.DefaultDisable;
    }
}