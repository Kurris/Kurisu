using System.Diagnostics.CodeAnalysis;
using Kurisu.AspNetCore.Abstractions.DataAccess.Core.Context;
using Kurisu.AspNetCore.Abstractions.Startup;
using Kurisu.Extensions.SqlSugar.Sharding;
using Kurisu.Extensions.SqlSugar.Utils;
using Kurisu.Test.DataAccess.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Test.DataAccess;

[Trait("Db", "Sharding")]
public class TestSharding
{
    private const string TenantId = "ut-sharding-tenant";
    private const string TableSuffix = "ut01";
    private readonly IServiceProvider _sp;

    [ExcludeFromCodeCoverage]
    public TestSharding()
    {
        _sp = TestHelper.GetServiceProvider(TenantId, enableSharding: true);
    }

    [Fact(DisplayName = "分表CRUD: 插入后分表可查询, 更新后数据变更, 删除后分表为空")]
    public async Task ShardingCrud_UsesShardTable()
    {
        using var scope = _sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                await PrepareShardingAsync(ctx);

                var entity = new TestShardingEntity
                {
                    Name = "before-update",
                    Age = 18
                };

                await ctx.InsertAsync(entity);

                var queried = await ctx.Queryable<TestShardingEntity>().SingleAsync(x => x.Id == entity.Id);
                Assert.Equal("before-update", queried.Name);

                queried.Name = "after-update";
                queried.Age = 20;
                await ctx.UpdateAsync(queried);

                var shardRows = await QueryShardTableAsync(ctx);
                Assert.Single(shardRows);
                Assert.Equal("after-update", shardRows[0].Name);
                Assert.Equal(TenantId, shardRows[0].TenantId);

                await ctx.DeleteAsync(queried, true);
                shardRows = await QueryShardTableAsync(ctx);
                Assert.Empty(shardRows);
            }
        }
    }

    [Fact(DisplayName = "忽略分表: 查询仅命中基表数据, 分表数据不可见")]
    public async Task IgnoreSharding_QueriesBaseTable()
    {
        using var scope = _sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                await PrepareShardingAsync(ctx, createBaseTable: true);

                var client = ctx.AsSqlSugarDbContext().GetClient();
                var baseTableName = client.EntityMaintenance.GetTableName<TestShardingEntity>();

                await client.Insertable(new TestShardingEntity
                {
                    Name = "base-record",
                    Age = 30,
                    TenantId = TenantId
                }).AS(baseTableName).ExecuteCommandIdentityIntoEntityAsync();

                await ctx.InsertAsync(new TestShardingEntity
                {
                    Name = "shard-record",
                    Age = 31
                });

                using (ctx.IgnoreSharding())
                {
                    var baseRows = await ctx.Queryable<TestShardingEntity>().ToListAsync();
                    Assert.Single(baseRows);
                    Assert.Equal("base-record", baseRows[0].Name);
                }
            }
        }
    }

    [Fact(DisplayName = "同步批量插入: 数据仅写入分表, 忽略分表后基表为空")]
    public async Task SyncBatchInsert_WritesShardTableOnly()
    {
        using var scope = _sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                await PrepareShardingAsync(ctx, createBaseTable: true);

                var entities = new List<TestShardingEntity>
                {
                    new() { Name = "batch-1", Age = 21 },
                    new() { Name = "batch-2", Age = 22 }
                };

                var inserted = ctx.Insert(entities);
                Assert.True(inserted);

                var shardRows = await QueryShardTableAsync(ctx);
                Assert.Equal(2, shardRows.Count);
                Assert.All(shardRows, row => Assert.Equal(TenantId, row.TenantId));

                using (ctx.IgnoreSharding())
                {
                    var baseRows = await ctx.Queryable<TestShardingEntity>().ToListAsync();
                    Assert.Empty(baseRows);
                }
            }
        }
    }

    [Fact(DisplayName = "UseTenant分表: 实体租户为空时按UseTenant租户路由")]
    public async Task UseTenant_EmptyEntityTenant_UsesScopedTenantForRoute()
    {
        var sp = TestHelper.GetServiceProvider("other-tenant", enableSharding: true);
        using var scope = sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                await PrepareShardingAsync(ctx);

                using (ctx.UseTenant(TenantId))
                {
                    await ctx.InsertAsync(new TestShardingEntity
                    {
                        Name = "use-tenant-shard",
                        Age = 32
                    });
                }

                using (ctx.UseTenant(TenantId))
                {
                    var shardRows = await QueryShardTableAsync(ctx);
                    Assert.Single(shardRows);
                    Assert.Equal(TenantId, shardRows[0].TenantId);
                    Assert.Equal("use-tenant-shard", shardRows[0].Name);
                }
            }
        }
    }

    private static async Task<List<TestShardingEntity>> QueryShardTableAsync(IDbContext ctx)
    {
        var client = ctx.AsSqlSugarDbContext().GetClient();
        var shardTableName = $"{client.EntityMaintenance.GetTableName<TestShardingEntity>()}_{TableSuffix}";
        return await client.Queryable<TestShardingEntity>().AS(shardTableName).OrderBy(x => x.Id).ToListAsync();
    }

    private static async Task PrepareShardingAsync(IDbContext ctx, bool createBaseTable = false)
    {
        var client = ctx.AsSqlSugarDbContext().GetClient();
        var baseTableName = client.EntityMaintenance.GetTableName<TestShardingEntity>();
        var shardTableName = $"{baseTableName}_{TableSuffix}";

        client.CodeFirst.InitTables<ShardingRouteTable>();
        await client.Deleteable<ShardingRouteTable>().Where(x => x.TenantId == TenantId).ExecuteCommandAsync();
        await client.Insertable(new ShardingRouteTable
        {
            TenantId = TenantId,
            TableSuffix = TableSuffix
        }).ExecuteCommandAsync();

        await client.Ado.ExecuteCommandAsync($"DROP TABLE IF EXISTS `{shardTableName}`");
        await client.Ado.ExecuteCommandAsync($"DROP TABLE IF EXISTS `{baseTableName}`");

        ctx.CodeFirst.EnsureTablesExists(typeof(TestShardingEntity));

        if (createBaseTable)
        {
            client.CodeFirst.InitTables<TestShardingEntity>();
            await client.Ado.ExecuteCommandAsync($"DELETE FROM `{baseTableName}`");
        }

        Assert.True(client.DbMaintenance.IsAnyTable(shardTableName, false));
        await client.Ado.ExecuteCommandAsync($"DELETE FROM `{shardTableName}`");
    }
}
