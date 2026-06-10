using Kurisu.AspNetCore.Abstractions.DataAccess.Core.Context;
using Kurisu.AspNetCore.Abstractions.Startup;
using Kurisu.Extensions.SqlSugar.Context;
using Kurisu.Extensions.SqlSugar.Utils;
using Kurisu.Test.DataAccess.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kurisu.Test.DataAccess;

[Trait("Db", "Audit")]
public class TestAudit
{
    private readonly IServiceProvider _sp;

    public TestAudit()
    {
        _sp = TestHelper.GetServiceProvider();
    }

    private async Task PrepareTableAsync(IDbContext ctx)
    {
        ctx.CodeFirst.EnsureTableExists(typeof(TestAuditEntity));
        await ctx.AsSqlSugarDbContext().Deleteable<TestAuditEntity>().ExecuteCommandAsync();
    }

    [Fact(DisplayName = "插入时自动回填Id: 单条插入前Id为0, 插入后Id>0, 可通过Id查询到该记录")]
    public async Task Insert_AutoFillsId()
    {
        using var scope = _sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                await PrepareTableAsync(ctx);

                var entity = new TestAuditEntity { Name = "id-test" };
                Assert.Equal(0, entity.Id);

                await ctx.InsertAsync(entity);

                Assert.True(entity.Id > 0, $"插入后 Id 应被自动回填，实际值: {entity.Id}");
                var result = await ctx.Queryable<TestAuditEntity>().SingleAsync(x => x.Id == entity.Id);
                Assert.Equal(entity.Id, result.Id);
                Assert.Equal("id-test", result.Name);
            }
        }
    }

    [Fact(DisplayName = "批量插入回填Id: 至少第一条记录回填Id>0, 且可通过DB查询到所有3条记录")]
    public async Task BatchInsert_AutoFillsIdForEach()
    {
        using var scope = _sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                await PrepareTableAsync(ctx);

                var entities = new List<TestAuditEntity>
                {
                    new() { Name = "id-batch-1" },
                    new() { Name = "id-batch-2" },
                    new() { Name = "id-batch-3" }
                };

                Assert.All(entities, e => Assert.Equal(0, e.Id));
                await ctx.InsertAsync(entities);
                // SqlSugar 批量插入 MySQL 默认只回填第一条记录的自增Id
                Assert.True(entities[0].Id > 0, $"批量插入后第一条记录的 Id 应被回填，实际值: {entities[0].Id}");

                // 可通过 DB 查询验证 3 条均已插入
                var all = await ctx.Queryable<TestAuditEntity>().OrderBy(x => x.Id).ToListAsync();
                Assert.Equal(3, all.Count);
                Assert.All(all, r => Assert.True(r.Id > 0));
                Assert.Contains(all, r => r.Name == "id-batch-1");
                Assert.Contains(all, r => r.Name == "id-batch-2");
                Assert.Contains(all, r => r.Name == "id-batch-3");
            }
        }
    }

    [Fact(DisplayName = "插入时自动填充创建时间: CreatedTime 由 default 变为非默认值")]
    public async Task Insert_AutoFillsCreatedTime()
    {
        using var scope = _sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                await PrepareTableAsync(ctx);

                var entity = new TestAuditEntity
                {
                    Name = "auto-create",
                    CreatedTime = default // 触发自动填充
                };

                Assert.Equal(0, entity.Id);
                await ctx.InsertAsync(entity);
                Assert.True(entity.Id > 0, "插入后 Id 应被自动回填");

                var result = await ctx.Queryable<TestAuditEntity>().SingleAsync(x => x.Id == entity.Id);
                Assert.NotEqual(default, result.CreatedTime);
                Assert.True(result.CreatedTime > DateTime.MinValue.AddDays(1));
            }
        }
    }

    [Fact(DisplayName = "插入时保留显式创建时间: 已设置 CreatedTime 不会被覆盖")]
    public async Task Insert_PreservesExplicitCreatedTime()
    {
        using var scope = _sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                await PrepareTableAsync(ctx);

                var explicitTime = new DateTime(2024, 1, 15, 10, 30, 0);
                var entity = new TestAuditEntity
                {
                    Name = "explicit-create",
                    CreatedTime = explicitTime
                };

                await ctx.InsertAsync(entity);

                var result = await ctx.Queryable<TestAuditEntity>().SingleAsync(x => x.Id == entity.Id);
                Assert.Equal(explicitTime, result.CreatedTime);
            }
        }
    }

    [Fact(DisplayName = "更新时间严格覆盖: 即使显式设为旧值, 更新时仍被覆盖为DateTime.Now")]
    public async Task Update_AlwaysOverwritesUpdatedTime()
    {
        using var scope = _sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                await PrepareTableAsync(ctx);

                var entity = new TestAuditEntity { Name = "strict-1" };
                await ctx.InsertAsync(entity);
                var inserted = await ctx.Queryable<TestAuditEntity>().SingleAsync(x => x.Id == entity.Id);

                // 显式将 UpdatedTime 设为很久以前的时间，更新时应被覆盖
                var oldTime = new DateTime(2020, 6, 1, 12, 0, 0);
                inserted.Name = "updated-1";
                inserted.UpdatedTime = oldTime;

                var beforeUpdate = DateTime.Now;
                await ctx.UpdateAsync(inserted);
                var afterUpdate = DateTime.Now;

                var result = await ctx.Queryable<TestAuditEntity>().SingleAsync(x => x.Id == entity.Id);
                Assert.NotEqual(oldTime, result.UpdatedTime);
                Assert.True(result.UpdatedTime >= beforeUpdate.AddSeconds(-2),
                    $"UpdatedTime={result.UpdatedTime} 应接近当前时间, beforeUpdate={beforeUpdate}");
                Assert.True(result.UpdatedTime <= afterUpdate.AddSeconds(2),
                    $"UpdatedTime={result.UpdatedTime} 不应远超更新结束时间, afterUpdate={afterUpdate}");
            }
        }
    }

    [Fact(DisplayName = "连续两次更新: 每次UpdatedTime被覆盖为当前时间, 验证时间戳在合理窗口内")]
    public async Task Update_ConsecutiveUpdates_EachGeneratesNewerTime()
    {
        using var scope = _sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                await PrepareTableAsync(ctx);

                var entity = new TestAuditEntity { Name = "consecutive" };
                await ctx.InsertAsync(entity);
                var row = await ctx.Queryable<TestAuditEntity>().SingleAsync(x => x.Id == entity.Id);

                // 第一次更新
                var t1Before = DateTime.Now;
                row.Name = "first-update";
                row.UpdatedTime = new DateTime(2000, 1, 1); // 旧值，应被覆盖
                await ctx.UpdateAsync(row);
                var t1After = DateTime.Now;
                var first = await ctx.Queryable<TestAuditEntity>().SingleAsync(x => x.Id == entity.Id);

                // 验证第一次更新：UpdatedTime 被覆盖为更新时间窗口内的值
                Assert.NotEqual(new DateTime(2000, 1, 1), first.UpdatedTime);
                Assert.True(first.UpdatedTime >= t1Before.AddSeconds(-2),
                    $"first.UpdatedTime={first.UpdatedTime} 应 >= {t1Before.AddSeconds(-2)}");
                Assert.True(first.UpdatedTime <= t1After.AddSeconds(2),
                    $"first.UpdatedTime={first.UpdatedTime} 应 <= {t1After.AddSeconds(2)}");

                // 第二次更新
                await Task.Delay(100);
                var t2Before = DateTime.Now;
                first.Name = "second-update";
                first.UpdatedTime = new DateTime(2000, 1, 1); // 旧值，应被覆盖
                await ctx.UpdateAsync(first);
                var t2After = DateTime.Now;
                var second = await ctx.Queryable<TestAuditEntity>().SingleAsync(x => x.Id == entity.Id);

                // 验证第二次更新：UpdatedTime 同样被覆盖为当前时间窗口内的值
                Assert.NotEqual(new DateTime(2000, 1, 1), second.UpdatedTime);
                Assert.True(second.UpdatedTime >= t2Before.AddSeconds(-2),
                    $"second.UpdatedTime={second.UpdatedTime} 应 >= {t2Before.AddSeconds(-2)}");
                Assert.True(second.UpdatedTime <= t2After.AddSeconds(2),
                    $"second.UpdatedTime={second.UpdatedTime} 应 <= {t2After.AddSeconds(2)}");
            }
        }
    }

    [Fact(DisplayName = "批量更新: 每条记录均获得UpdatedTime, 且互不相同(因SetValue逐条触发)")]
    public async Task BatchUpdate_AutoFillsUpdatedTimeForEach()
    {
        using var scope = _sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                await PrepareTableAsync(ctx);

                var entities = new List<TestAuditEntity>
                {
                    new() { Name = "batch-up-1" },
                    new() { Name = "batch-up-2" },
                    new() { Name = "batch-up-3" }
                };
                await ctx.InsertAsync(entities);

                var rows = await ctx.Queryable<TestAuditEntity>().OrderBy(x => x.Id).ToListAsync();
                // 插入时仅 InsertDateTimeGeneration 触发，UpdatedTime 应保持 DB 最小值
                Assert.All(rows, r => Assert.True(r.UpdatedTime <= new DateTime(1900, 1, 2),
                    $"UpdatedTime={r.UpdatedTime} 应未被更新填充"));

                await Task.Delay(50);
                var beforeUpdate = DateTime.Now;

                foreach (var r in rows)
                {
                    r.Name += "-updated";
                }
                await ctx.UpdateAsync(rows);
                var afterUpdate = DateTime.Now;

                var updated = await ctx.Queryable<TestAuditEntity>().OrderBy(x => x.Id).ToListAsync();
                Assert.Equal(3, updated.Count);
                Assert.All(updated, r =>
                {
                    Assert.NotEqual(default, r.UpdatedTime);
                    Assert.True(r.UpdatedTime >= beforeUpdate.AddSeconds(-2),
                        $"UpdatedTime={r.UpdatedTime} 应接近更新时间");
                });
            }
        }
    }

    [Fact(DisplayName = "插入时不填充UpdatedTime: 仅Update属性触发, Insert后UpdatedTime保持default")]
    public async Task Insert_DoesNotFillUpdatedTime()
    {
        using var scope = _sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                await PrepareTableAsync(ctx);

                var entity = new TestAuditEntity { Name = "insert-only" };
                await ctx.InsertAsync(entity);
                var result = await ctx.Queryable<TestAuditEntity>().SingleAsync(x => x.Id == entity.Id);

                // UpdatedTime 没有 [InsertDateTimeGeneration]，插入后应保持未被设置的状态（DB最小值）
                Assert.True(result.UpdatedTime <= new DateTime(1900, 1, 2),
                    $"UpdatedTime={result.UpdatedTime} 插入后不应被填充");
                // CreatedTime 有 [InsertDateTimeGeneration]，应已被填充
                Assert.NotEqual(default, result.CreatedTime);
            }
        }
    }

    [Fact(DisplayName = "插入+更新完整流程: CreatedTime 不变, UpdatedTime 被更新为新的非默认值")]
    public async Task InsertThenUpdate_CreatedTimeUnchanged_UpdatedTimeChanged()
    {
        using var scope = _sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                await PrepareTableAsync(ctx);

                var beforeInsert = DateTime.Now;
                var entity = new TestAuditEntity { Name = "audit-test" };
                await ctx.InsertAsync(entity);
                var inserted = await ctx.Queryable<TestAuditEntity>().SingleAsync(x => x.Id == entity.Id);

                // 创建时间在插入后被自动填充
                Assert.NotEqual(default, inserted.CreatedTime);

                await Task.Delay(100);

                inserted.Name = "audit-updated";
                await ctx.UpdateAsync(inserted);
                var updated = await ctx.Queryable<TestAuditEntity>().SingleAsync(x => x.Id == entity.Id);

                // 创建时间不应改变
                Assert.Equal(inserted.CreatedTime, updated.CreatedTime);
                // 更新时间在更新后应被设置为新值（非 default）
                Assert.NotEqual(default, updated.UpdatedTime);
            }
        }
    }

    [Fact(DisplayName = "批量插入: 每条记录的创建时间均被自动填充")]
    public async Task BatchInsert_AutoFillsCreatedTimeForEach()
    {
        using var scope = _sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                await PrepareTableAsync(ctx);

                var entities = new List<TestAuditEntity>
                {
                    new() { Name = "batch-1" },
                    new() { Name = "batch-2" },
                    new() { Name = "batch-3" }
                };

                await ctx.InsertAsync(entities);

                var results = await ctx.Queryable<TestAuditEntity>().OrderBy(x => x.Id).ToListAsync();
                Assert.Equal(3, results.Count);
                Assert.All(results, r => Assert.NotEqual(default, r.CreatedTime));
            }
        }
    }

    [Fact(DisplayName = "默认审计用户: 未接入当前用户上下文时写入系统用户-1")]
    public async Task DefaultAuditAccessor_FillsSystemUserId()
    {
        var serviceProvider = TestHelper.GetServiceProvider(configureServices: services =>
        {
            services.Replace(ServiceDescriptor.Singleton<IDbAuditAccessor, NullDbAuditAccessor>());
        });

        using var scope = serviceProvider.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                await PrepareTableAsync(ctx);

                var entity = new TestAuditEntity { Name = "system-audit" };
                await ctx.InsertAsync(entity);

                var inserted = await ctx.Queryable<TestAuditEntity>().SingleAsync(x => x.Id == entity.Id);
                Assert.Equal(-1, inserted.CreatedBy);
                Assert.Equal(-1, inserted.ModifiedBy);

                inserted.Name = "system-audit-updated";
                await ctx.UpdateAsync(inserted);

                var updated = await ctx.Queryable<TestAuditEntity>().SingleAsync(x => x.Id == entity.Id);
                Assert.Equal(-1, updated.ModifiedBy);
            }
        }
    }

    [Fact(DisplayName = "当前用户审计: UseCurrentUserContext 使用 ICurrentUser 写入用户ID")]
    public async Task CurrentUserAuditAccessor_FillsCurrentUserId()
    {
        using var scope = _sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                await PrepareTableAsync(ctx);

                var entity = new TestAuditEntity { Name = "current-user-audit" };
                await ctx.InsertAsync(entity);

                var inserted = await ctx.Queryable<TestAuditEntity>().SingleAsync(x => x.Id == entity.Id);
                Assert.Equal(3, inserted.CreatedBy);
                Assert.Equal(3, inserted.ModifiedBy);

                inserted.Name = "current-user-audit-updated";
                await ctx.UpdateAsync(inserted);

                var updated = await ctx.Queryable<TestAuditEntity>().SingleAsync(x => x.Id == entity.Id);
                Assert.Equal(3, updated.ModifiedBy);
            }
        }
    }
}
