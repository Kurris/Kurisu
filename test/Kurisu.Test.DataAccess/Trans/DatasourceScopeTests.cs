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
///
/// 核心原理：
///   每次调用 CreateDatasourceScope / [Datasource] 都会将新的 Transient IDatasourceManager（含独立 ISqlSugarClient）
///   推入 IDbContext 内部的管理器栈；Dispose 时弹出，自动回退到上一层的数据源与事务上下文。
///   因此同一请求中的多个数据源作用域使用不同的数据库连接，事务完全隔离，互不干扰。
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

    // ─────────────────────────────────────────────
    //  辅助方法
    // ─────────────────────────────────────────────

    /// <summary>建表（不存在时）并清空数据</summary>
    private async Task PrepareTableAsync(IDbContext ctx)
    {
        ctx.CodeFirst.EnsureTableExists(typeof(TxTest));
        await ctx.AsSqlSugarDbContext().Deleteable<TxTest>().ExecuteCommandAsync();
    }

    /// <summary>统计指定 name 的行数</summary>
    private async Task<int> CountAsync(IDbContext ctx, string name)
        => await ctx.Queryable<TxTest>().CountAsync(x => x.Name == name);

    // ─────────────────────────────────────────────
    //  1. 数据源作用域栈基础行为
    // ─────────────────────────────────────────────

    /// <summary>
    /// CreateDatasourceScope 推入新管理器后，Client 指向该新连接；
    /// Dispose 后 Client 恢复到外层管理器。
    ///
    /// 验证点：内层用不同数据源名（SecondConnectionString）推入新管理器，
    /// 与外层（DefaultConnectionString）管理器为不同实例（独立连接）；
    /// 内层 Dispose 后恢复为外层管理器实例。
    /// 注意：ctx.DatasourceManager 仅在 CreateDatasourceScope 内有效，否则抛异常。
    /// </summary>
    [Fact]
    public async Task CreateDatasourceScope_PushesNewManager_RestoredAfterDispose()
    {
        using var scope = _sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();

            using (ctx.CreateDatasourceScope())
            {
                await PrepareTableAsync(ctx);

                // 记录外层管理器实例
                var outerManager = ctx.DatasourceManager;

                using (ctx.CreateDatasourceScope("SecondConnectionString"))
                {
                    // 推入不同数据源后，DatasourceManager 应为新实例
                    var innerManager = ctx.DatasourceManager;
                    Assert.NotSame(outerManager, innerManager);

                    // 在新连接上插入（自动提交）
                    await ctx.InsertAsync(new TxTest { Name = "inner_scope" });
                }

                // Dispose 后，DatasourceManager 恢复为外层实例
                Assert.Same(outerManager, ctx.DatasourceManager);

                // 内层已自动提交，数据可见
                Assert.Equal(1, await CountAsync(ctx, "inner_scope"));
            }
        }
    }

    /// <summary>
    /// 嵌套两层 CreateDatasourceScope：外层（DefaultConnectionString）与内层（SecondConnectionString）
    /// 使用不同数据源，各自独立，内层弹出后管理器正确回退至外层。
    ///
    /// 注意：同名数据源的嵌套 CreateDatasourceScope 是 no-op（复用当前连接与事务），
    /// 因此两层隔离测试必须使用不同的数据源名称。
    /// </summary>
    [Fact]
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
                    // 推入第二层，管理器应与 layer1 不同
                    var layer2 = ctx.DatasourceManager;
                    Assert.NotSame(layer1, layer2);

                    await ctx.InsertAsync(new TxTest { Name = "layer2" });
                }

                // 弹出第二层，应回到 layer1
                Assert.Same(layer1, ctx.DatasourceManager);

                // 两层数据均已自动提交，可见
                Assert.Equal(1, await CountAsync(ctx, "layer1"));
                Assert.Equal(1, await CountAsync(ctx, "layer2"));
            }
        }
    }

    // ─────────────────────────────────────────────
    //  2. 数据源作用域内的事务边界
    // ─────────────────────────────────────────────

    /// <summary>
    /// 外层管理器（栈底）持有事务时，CreateDatasourceScope 推入新管理器。
    /// 新管理器在独立连接上直接插入（无事务），立即自动提交。
    /// 外层事务回滚后，内层已提交数据不受影响，依然可查询到。
    /// </summary>
    [Fact]
    public async Task OuterTransaction_Rollback_DoesNotAffect_InnerScopeAutoCommit()
    {
        using var scope = _sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                await PrepareTableAsync(ctx);

                // 外层：手动开启事务
                using var outerTrans = ctx.DatasourceManager.CreateTransScope(Kurisu.AspNetCore.Abstractions.DataAccess.Propagation.Required);
                await outerTrans.BeginAsync();

                // 外层插入（将随外层事务回滚）
                await ctx.InsertAsync(new TxTest { Name = "outer_will_rollback" });

                // 内层：推入独立连接，直接插入（AutoCloseConnection = true → 自动提交）
                using (ctx.CreateDatasourceScope("SecondConnectionString"))
                {
                    await ctx.InsertAsync(new TxTest { Name = "inner_auto_commit" });
                }

                // 外层事务回滚
                await outerTrans.RollbackAsync();

                // 外层数据已回滚
                Assert.Equal(0, await CountAsync(ctx, "outer_will_rollback"));

                // 内层数据已独立提交，不受外层回滚影响
                Assert.Equal(1, await CountAsync(ctx, "inner_auto_commit"));
            }
        }
    }

    /// <summary>
    /// 外层管理器（栈底）持有事务时，CreateDatasourceScope 推入新管理器，
    /// 在新管理器上开启事务并提交；外层事务回滚不影响内层已提交的数据。
    /// </summary>
    [Fact]
    public async Task OuterTransaction_Rollback_DoesNotAffect_InnerScopeCommit()
    {
        using var scope = _sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                await PrepareTableAsync(ctx);

                // 外层事务
                using var outerTrans = ctx.DatasourceManager.CreateTransScope(Kurisu.AspNetCore.Abstractions.DataAccess.Propagation.Required);
                await outerTrans.BeginAsync();
                await ctx.InsertAsync(new TxTest { Name = "outer_will_rollback" });

                // 内层：推入独立连接，开启独立事务，提交
                using (ctx.CreateDatasourceScope("SecondConnectionString"))
                {
                    using var innerTrans = ctx.DatasourceManager.CreateTransScope(Kurisu.AspNetCore.Abstractions.DataAccess.Propagation.Required);
                    await innerTrans.BeginAsync();
                    await ctx.InsertAsync(new TxTest { Name = "inner_committed" });
                    await innerTrans.CommitAsync();
                }

                // 外层回滚
                await outerTrans.RollbackAsync();

                Assert.Equal(0, await CountAsync(ctx, "outer_will_rollback"));
                Assert.Equal(1, await CountAsync(ctx, "inner_committed"));
            }
        }
    }

    /// <summary>
    /// 外层管理器（栈底）持有事务时，推入内层作用域后内层事务回滚，
    /// 外层事务正常提交，外层数据可见，内层回滚数据不可见。
    /// </summary>
    [Fact]
    public async Task InnerScopeTransaction_Rollback_DoesNotAffect_OuterTransaction_Commit()
    {
        using var scope = _sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                await PrepareTableAsync(ctx);

                // 外层事务
                using var outerTrans = ctx.DatasourceManager.CreateTransScope(Kurisu.AspNetCore.Abstractions.DataAccess.Propagation.Required);
                await outerTrans.BeginAsync();
                await ctx.InsertAsync(new TxTest { Name = "outer_commits" });

                // 内层：开启独立事务，回滚
                using (ctx.CreateDatasourceScope("SecondConnectionString"))
                {
                    using var innerTrans = ctx.DatasourceManager.CreateTransScope(Kurisu.AspNetCore.Abstractions.DataAccess.Propagation.Required);
                    await innerTrans.BeginAsync();
                    await ctx.InsertAsync(new TxTest { Name = "inner_rollback" });
                    await innerTrans.RollbackAsync();
                }

                // 外层提交
                await outerTrans.CommitAsync();

                Assert.Equal(1, await CountAsync(ctx, "outer_commits"));
                Assert.Equal(0, await CountAsync(ctx, "inner_rollback"));
            }
        }
    }

    // ─────────────────────────────────────────────
    //  3. [Datasource] 属性隔离行为
    // ─────────────────────────────────────────────

    /// <summary>
    /// [Datasource] 方法内直接插入（无事务）：
    /// 使用独立连接自动提交，外层环境无事务，数据正常可见。
    /// </summary>
    [Fact]
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

    /// <summary>
    /// [Datasource] + [Transactional] 方法成功提交：
    /// 在独立连接上开启事务并提交，数据正常可见。
    /// </summary>
    [Fact]
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

    /// <summary>
    /// [Datasource] + [Transactional] 方法抛出异常：
    /// 内层事务回滚，数据不可见。外层管理器（栈底）未受影响。
    /// </summary>
    [Fact]
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
                // 内层已回滚，数据不可见
                Assert.Equal(0, await CountAsync(ctx, "ds_tx_rollback"));

                // 外层管理器（栈底）依然可正常使用
                await ctx.InsertAsync(new TxTest { Name = "outer_still_works" });
                Assert.Equal(1, await CountAsync(ctx, "outer_still_works"));
            }
        }
    }

    /// <summary>
    /// 外层 [Transactional] 回滚时，内层 [Datasource] 已独立提交的数据不被回滚。
    ///
    /// 验证点：
    ///   - outerName 随外层事务回滚，数据不可见（count == 0）
    ///   - innerName 由内层独立连接插入并自动提交，不受外层回滚影响（count == 1）
    /// </summary>
    [Fact]
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

                // 外层因异常回滚
                Assert.Equal(0, await CountAsync(ctx, "outer_rollback"));

                // 内层 [Datasource] 独立提交，不受外层回滚影响
                Assert.Equal(1, await CountAsync(ctx, "inner_survive"));
            }
        }
    }

    // ─────────────────────────────────────────────
    //  4. 同一 IDbContext 两个数据源作用域的并行事务
    // ─────────────────────────────────────────────

    /// <summary>
    /// 在同一请求作用域中：先用外层管理器插入并提交，
    /// 再用推入的内层管理器插入并提交。两组数据均可见。
    /// 验证栈上不同层的管理器事务互不干扰。
    /// </summary>
    [Fact]
    public async Task TwoScopedManagers_BothCommit_BothVisible()
    {
        using var scope = _sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                await PrepareTableAsync(ctx);

                // 外层管理器插入并提交（默认连接）
                using var outerTrans = ctx.DatasourceManager.CreateTransScope(Kurisu.AspNetCore.Abstractions.DataAccess.Propagation.Required);
                await outerTrans.BeginAsync();
                await ctx.InsertAsync(new TxTest { Name = "manager_outer" });
                await outerTrans.CommitAsync();

                // 内层管理器插入并提交（独立连接）
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

    /// <summary>
    /// 两个独立请求作用域（各自的 IDbContext）完全隔离，互不影响。
    /// 验证 Transient IDatasourceManager 使得不同 IDbContext 之间无共享状态。
    /// </summary>
    [Fact]
    public async Task TwoScopes_DifferentDbContexts_CompletelyIsolated()
    {
        object? scope1Manager = null;
        object? scope2Manager = null;

        // 第一个请求作用域
        using var scope1 = _sp.CreateScope();
        using (scope1.ServiceProvider.InitLifecycle())
        {
            var ctx1 = scope1.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx1.CreateDatasourceScope())
            {
                await PrepareTableAsync(ctx1);
                scope1Manager = ctx1.DatasourceManager;

                // scope1 开启事务，插入但不提交
                using var trans1 = ctx1.DatasourceManager.CreateTransScope(Kurisu.AspNetCore.Abstractions.DataAccess.Propagation.Required);
                await trans1.BeginAsync();
                await ctx1.InsertAsync(new TxTest { Name = "scope1_uncommitted" });

                // scope1 回滚
                await trans1.RollbackAsync();

                // scope1 数据已回滚
                Assert.Equal(0, await CountAsync(ctx1, "scope1_uncommitted"));
            }
        }

        // 第二个请求作用域（独立 IDbContext，独立 IDatasourceManager）
        using var scope2 = _sp.CreateScope();
        using (scope2.ServiceProvider.InitLifecycle())
        {
            var ctx2 = scope2.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx2.CreateDatasourceScope())
            {
                scope2Manager = ctx2.DatasourceManager;

                // scope2 插入并提交
                using var trans2 = ctx2.DatasourceManager.CreateTransScope(Kurisu.AspNetCore.Abstractions.DataAccess.Propagation.Required);
                await trans2.BeginAsync();
                await ctx2.InsertAsync(new TxTest { Name = "scope2_committed" });
                await trans2.CommitAsync();

                Assert.Equal(1, await CountAsync(ctx2, "scope2_committed"));
            }
        }

        // 两个作用域的管理器实例应完全不同
        Assert.NotNull(scope1Manager);
        Assert.NotNull(scope2Manager);
        Assert.NotSame(scope1Manager, scope2Manager);
    }
}
