using Kurisu.EFSharding.Core.VirtualRoutes.TableRoutes.RouteTails.Abstractions;
using Kurisu.EFSharding.Sharding.Abstractions;
using Kurisu.EFSharding.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Kurisu.EFSharding.Sharding;

public abstract class BaseShardingDbContext : DbContext, IShardingDbContext //, IShardingTableDbContext
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="options"></param>
    protected BaseShardingDbContext(DbContextOptions options) : base(options)
    {
    }

    private IShardingDbContextExecutor _shardingDbContextExecutor;

    /// <summary>
    /// 分片执行者
    /// </summary>
    /// <returns></returns>
    public IShardingDbContextExecutor GetShardingExecutor()
    {
        return _shardingDbContextExecutor ??= this.CreateShardingDbContextExecutor();
    }

    public IRouteTail RouteTail { get; set; }

    public override void Dispose()
    {
        _shardingDbContextExecutor?.Dispose();
        base.Dispose();
    }

    public override async ValueTask DisposeAsync()
    {
        if (_shardingDbContextExecutor != null)
        {
            await _shardingDbContextExecutor.DisposeAsync();
        }

        await base.DisposeAsync();
    }
}