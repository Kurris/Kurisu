using System.Diagnostics.CodeAnalysis;
using Kurisu.AspNetCore.Abstractions.DataAccess;
using Kurisu.AspNetCore.Abstractions.DataAccess.Core.Context;
using Kurisu.AspNetCore.Abstractions.Startup;
using Kurisu.Extensions.SqlSugar.Utils;
using Kurisu.Test.DataAccess.Trans.Mock;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Test.DataAccess.Trans;

/// <summary>
/// 验证数据源作用域（CreateDatasourceScope / [Datasource]）与事务边界的隔离行为。
/// 每次 CreateDatasourceScope 推入新 ISqlSugarClient 到管理器栈，Dispose 时弹出回退。
/// </summary>
[Trait("Db", "DatasourceScope")]
public class DatasourceScopeTests
{
    private readonly IServiceProvider _sp;

    [ExcludeFromCodeCoverage]
    public DatasourceScopeTests()
    {
        _sp = TestHelper.GetServiceProvider();
    }

    private async Task PrepareTableAsync(IDbContext ctx)
    {
        ctx.CodeFirst.EnsureTableExists(typeof(TxTest));
        await ctx.AsSqlSugarDbContext().Deleteable<TxTest>().ExecuteCommandAsync();
    }

    private async Task<int> CountAsync(IDbContext ctx, string name)
        => await ctx.Queryable<TxTest>().CountAsync(x => x.Name == name);

    // ── 数据源作用域栈基础行为 ──

    [Fact(DisplayName = "数据源作用域栈: 内层推入新管理器, Dispose后恢复到外层管理器实例")]
    public async Task CreateDatasourceScope_PushesNewManager_RestoredAfterDispose()
    {
        using var scope = _sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                await PrepareTableAsync(ctx);
                var outerManager = ctx.DatasourceManager;

                using (ctx.CreateDatasourceScope("SecondConnectionString"))
                {
                    var innerManager = ctx.DatasourceManager;
                    Assert.NotSame(outerManager, innerManager);
                    await ctx.InsertAsync(new TxTest { Name = "inner_scope" });
                }

                Assert.Same(outerManager, ctx.DatasourceManager);
                Assert.Equal(1, await CountAsync(ctx, "inner_scope"));
            }
        }
    }

    [Fact(DisplayName = "数据源作用域两层嵌套: 两层独立连接管理器, Dispose后逐层回退")]
    public async Task CreateDatasourceScope_Nested_TwoLayers_EachIndependent()
    {
        using var scope = _sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                var layer1 = ctx.DatasourceManager;
                await PrepareTableAsync(ctx);
                await ctx.InsertAsync(new TxTest { Name = "layer1" });

                using (ctx.CreateDatasourceScope("SecondConnectionString"))
                {
                    var layer2 = ctx.DatasourceManager;
                    Assert.NotSame(layer1, layer2);
                    await ctx.InsertAsync(new TxTest { Name = "layer2" });
                }

                Assert.Same(layer1, ctx.DatasourceManager);
                Assert.Equal(1, await CountAsync(ctx, "layer1"));
                Assert.Equal(1, await CountAsync(ctx, "layer2"));
            }
        }
    }

    // ── 数据源作用域内的事务边界 ──

    [Fact(DisplayName = "外层事务回滚不影响内层: 内层独立连接自动提交, 外层回滚后内层数据仍可见")]
    public async Task OuterTransaction_Rollback_DoesNotAffect_InnerScopeAutoCommit()
    {
        using var scope = _sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                await PrepareTableAsync(ctx);

                using var outerTrans = ctx.DatasourceManager.CreateTransScope(Kurisu.AspNetCore.Abstractions.DataAccess.Propagation.Required);
                await outerTrans.BeginAsync();
                await ctx.InsertAsync(new TxTest { Name = "outer_will_rollback" });

                using (ctx.CreateDatasourceScope("SecondConnectionString"))
                {
                    await ctx.InsertAsync(new TxTest { Name = "inner_auto_commit" });
                }

                await outerTrans.RollbackAsync();

                Assert.Equal(0, await CountAsync(ctx, "outer_will_rollback"));
                Assert.Equal(1, await CountAsync(ctx, "inner_auto_commit"));
            }
        }
    }

    [Fact(DisplayName = "内层独立事务提交: 内层独立事务提交后外层回滚不影响内层数据")]
    public async Task OuterTransaction_Rollback_DoesNotAffect_InnerScopeCommit()
    {
        using var scope = _sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                await PrepareTableAsync(ctx);

                using var outerTrans = ctx.DatasourceManager.CreateTransScope(Kurisu.AspNetCore.Abstractions.DataAccess.Propagation.Required);
                await outerTrans.BeginAsync();
                await ctx.InsertAsync(new TxTest { Name = "outer_will_rollback" });

                using (ctx.CreateDatasourceScope("SecondConnectionString"))
                {
                    using var innerTrans = ctx.DatasourceManager.CreateTransScope(Kurisu.AspNetCore.Abstractions.DataAccess.Propagation.Required);
                    await innerTrans.BeginAsync();
                    await ctx.InsertAsync(new TxTest { Name = "inner_committed" });
                    await innerTrans.CommitAsync();
                }

                await outerTrans.RollbackAsync();
                Assert.Equal(0, await CountAsync(ctx, "outer_will_rollback"));
                Assert.Equal(1, await CountAsync(ctx, "inner_committed"));
            }
        }
    }

    [Fact(DisplayName = "内层事务回滚不影响外层: 内层独立事务回滚, 外层提交成功")]
    public async Task InnerScopeTransaction_Rollback_DoesNotAffect_OuterTransaction_Commit()
    {
        using var scope = _sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                await PrepareTableAsync(ctx);

                using var outerTrans = ctx.DatasourceManager.CreateTransScope(Kurisu.AspNetCore.Abstractions.DataAccess.Propagation.Required);
                await outerTrans.BeginAsync();
                await ctx.InsertAsync(new TxTest { Name = "outer_commits" });

                using (ctx.CreateDatasourceScope("SecondConnectionString"))
                {
                    using var innerTrans = ctx.DatasourceManager.CreateTransScope(Kurisu.AspNetCore.Abstractions.DataAccess.Propagation.Required);
                    await innerTrans.BeginAsync();
                    await ctx.InsertAsync(new TxTest { Name = "inner_rollback" });
                    await innerTrans.RollbackAsync();
                }

                await outerTrans.CommitAsync();
                Assert.Equal(1, await CountAsync(ctx, "outer_commits"));
                Assert.Equal(0, await CountAsync(ctx, "inner_rollback"));
            }
        }
    }

    // ── [Datasource] 属性隔离行为 ──

    [Fact(DisplayName = "[Datasource]无事务: 独立连接自动提交, 数据可见")]
    public async Task Datasource_Insert_NoTransaction_AutoCommit()
    {
        using var scope = _sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            var svc = scope.ServiceProvider.GetRequiredService<IDatasourceScopeService>();
            using (ctx.CreateDatasourceScope())
            {
                await PrepareTableAsync(ctx);
            }

            await svc.InsertInDatasourceScopeAsync("ds_auto_commit");

            using (ctx.CreateDatasourceScope())
            {
                Assert.Equal(1, await CountAsync(ctx, "ds_auto_commit"));
            }
        }
    }

    [Fact(DisplayName = "[Datasource]+[Transactional]成功: 独立事务提交成功, 数据可见")]
    public async Task Datasource_WithTransaction_Commits_OnSuccess()
    {
        using var scope = _sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            var svc = scope.ServiceProvider.GetRequiredService<IDatasourceScopeService>();
            using (ctx.CreateDatasourceScope())
            {
                await PrepareTableAsync(ctx);
            }

            await svc.InsertInDatasourceScopeWithTransactionAsync("ds_tx_commit");

            using (ctx.CreateDatasourceScope())
            {
                Assert.Equal(1, await CountAsync(ctx, "ds_tx_commit"));
            }
        }
    }

    [Fact(DisplayName = "[Datasource]+[Transactional]异常: 内层事务回滚, 外层管理器不受影响")]
    public async Task Datasource_WithTransaction_Rollbacks_OnException()
    {
        using var scope = _sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            var svc = scope.ServiceProvider.GetRequiredService<IDatasourceScopeService>();
            using (ctx.CreateDatasourceScope())
            {
                await PrepareTableAsync(ctx);
            }

            await Assert.ThrowsAsync<Exception>(
                () => svc.InsertInDatasourceScopeWithTransactionAndThrowAsync("ds_tx_rollback"));

            using (ctx.CreateDatasourceScope())
            {
                Assert.Equal(0, await CountAsync(ctx, "ds_tx_rollback"));

                await ctx.InsertAsync(new TxTest { Name = "outer_still_works" });
                Assert.Equal(1, await CountAsync(ctx, "outer_still_works"));
            }
        }
    }

    [Fact(DisplayName = "外层事务回滚不影响内层[Datasource]: 内层独立提交, 数据不受外层回滚影响")]
    public async Task OuterTransactional_Rollback_DoesNotAffect_InnerDatasourceScope_AutoCommit()
    {
        using var scope = _sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            var svc = scope.ServiceProvider.GetRequiredService<IDatasourceScopeService>();
            using (ctx.CreateDatasourceScope())
            {
                await PrepareTableAsync(ctx);

                using var outerTrans = ctx.DatasourceManager.CreateTransScope(Kurisu.AspNetCore.Abstractions.DataAccess.Propagation.Required);
                await outerTrans.BeginAsync();

                await ctx.InsertAsync(new TxTest { Name = "outer_rollback" });
                await svc.InsertInSecondDatasourceScopeAsync("inner_survive");

                await outerTrans.RollbackAsync();

                Assert.Equal(0, await CountAsync(ctx, "outer_rollback"));
                Assert.Equal(1, await CountAsync(ctx, "inner_survive"));
            }
        }
    }

    // ── 两个数据源并行事务 ──

    [Fact(DisplayName = "两个作用域管理器各自提交: 栈上不同层管理器事务互不干扰, 双方数据均可见")]
    public async Task TwoScopedManagers_BothCommit_BothVisible()
    {
        using var scope = _sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                await PrepareTableAsync(ctx);

                using var outerTrans = ctx.DatasourceManager.CreateTransScope(Kurisu.AspNetCore.Abstractions.DataAccess.Propagation.Required);
                await outerTrans.BeginAsync();
                await ctx.InsertAsync(new TxTest { Name = "manager_outer" });
                await outerTrans.CommitAsync();

                using (ctx.CreateDatasourceScope())
                {
                    using var innerTrans = ctx.DatasourceManager.CreateTransScope(Kurisu.AspNetCore.Abstractions.DataAccess.Propagation.Required);
                    await innerTrans.BeginAsync();
                    await ctx.InsertAsync(new TxTest { Name = "manager_inner" });
                    await innerTrans.CommitAsync();
                }

                Assert.Equal(1, await CountAsync(ctx, "manager_outer"));
                Assert.Equal(1, await CountAsync(ctx, "manager_inner"));
            }
        }
    }

    [Fact(DisplayName = "两个请求作用域完全隔离: 不同IDbContext的管理器实例不同, 事务互不干扰")]
    public async Task TwoScopes_DifferentDbContexts_CompletelyIsolated()
    {
        object? scope1Manager = null;
        object? scope2Manager = null;

        using var scope1 = _sp.CreateScope();
        using (scope1.ServiceProvider.InitLifecycle())
        {
            var ctx1 = scope1.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx1.CreateDatasourceScope())
            {
                await PrepareTableAsync(ctx1);
                scope1Manager = ctx1.DatasourceManager;

                using var trans1 = ctx1.DatasourceManager.CreateTransScope(Kurisu.AspNetCore.Abstractions.DataAccess.Propagation.Required);
                await trans1.BeginAsync();
                await ctx1.InsertAsync(new TxTest { Name = "scope1_uncommitted" });
                await trans1.RollbackAsync();

                Assert.Equal(0, await CountAsync(ctx1, "scope1_uncommitted"));
            }
        }

        using var scope2 = _sp.CreateScope();
        using (scope2.ServiceProvider.InitLifecycle())
        {
            var ctx2 = scope2.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx2.CreateDatasourceScope())
            {
                scope2Manager = ctx2.DatasourceManager;

                using var trans2 = ctx2.DatasourceManager.CreateTransScope(Kurisu.AspNetCore.Abstractions.DataAccess.Propagation.Required);
                await trans2.BeginAsync();
                await ctx2.InsertAsync(new TxTest { Name = "scope2_committed" });
                await trans2.CommitAsync();

                Assert.Equal(1, await CountAsync(ctx2, "scope2_committed"));
            }
        }

        Assert.NotNull(scope1Manager);
        Assert.NotNull(scope2Manager);
        Assert.NotSame(scope1Manager, scope2Manager);
    }
}