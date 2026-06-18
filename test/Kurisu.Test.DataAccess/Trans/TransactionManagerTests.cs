using Kurisu.AspNetCore.Abstractions.DataAccess;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;
using Kurisu.AspNetCore.Abstractions.DataAccess.Core;
using Kurisu.Extensions.SqlSugar.Core.Manager;
using Kurisu.AspNetCore.Abstractions.Startup;
using Kurisu.AspNetCore.Abstractions.DataAccess.Core.Context;

namespace Kurisu.Test.DataAccess.Trans;

[Trait("Db", "Trans")]
public class TransactionManagerTests
{
    private readonly IServiceProvider _sp;

    public TransactionManagerTests()
    {
        _sp = TestHelper.GetServiceProvider();
    }

    /// <summary>
    /// 建表并清空数据
    /// </summary>
    private async Task PrepareTableAsync(IDatasourceManager manager)
    {
        var client = manager.GetCurrentClient<ISqlSugarClient>();
        if (client == null) throw new Exception("ISqlSugarClient not available from manager.CurrentDbClient");
        await client.Ado.ExecuteCommandAsync("CREATE TABLE IF NOT EXISTS tx_test (id INT AUTO_INCREMENT PRIMARY KEY, name VARCHAR(200));");
        await client.Ado.ExecuteCommandAsync("TRUNCATE TABLE tx_test;");
    }

    /// <summary>
    /// 统计指定 name 的行数
    /// </summary>
    private async Task<int> CountAsync(IDatasourceManager manager, string name)
    {
        var client = manager.GetCurrentClient<ISqlSugarClient>();
        if (client == null) throw new Exception("ISqlSugarClient not available from manager.CurrentDbClient");
        return await client.Ado.GetIntAsync("SELECT COUNT(1) FROM tx_test WHERE name = @name", new { name });
    }

    [Fact(DisplayName = "Required嵌套: 内层加入外层事务, 外层提交后内外数据均持久化")]
    public async Task Required_JoinsAmbientAndCommitByOuter_PersistsAllInnerAndOuterInserts()
    {
        var scope = _sp.CreateScope();
        var sp = scope.ServiceProvider;
        using (sp.InitLifecycle())
        {
            using (sp.GetService<IDbContext>().CreateDatasourceScope())
            {
                var manager = sp.GetRequiredService<IDatasourceManager>();
                await PrepareTableAsync(manager);

                using (var outer = manager.CreateTransScope(Propagation.Required))
                {
                    await outer.BeginAsync();
                    var outerClient = manager.GetCurrentClient<ISqlSugarClient>();
                    await outerClient!.Ado.ExecuteCommandAsync("INSERT INTO tx_test (name) VALUES (@name)", new { name = "outer" });

                    using (var inner = manager.CreateTransScope(Propagation.Required))
                    {
                        await inner.BeginAsync();
                        var innerClient = manager.GetCurrentClient<ISqlSugarClient>();
                        await innerClient!.Ado.ExecuteCommandAsync("INSERT INTO tx_test (name) VALUES (@name)", new { name = "inner" });
                        await inner.CommitAsync();
                    }

                    await outer.CommitAsync();
                }

                Assert.Equal(1, await CountAsync(manager, "outer"));
                Assert.Equal(1, await CountAsync(manager, "inner"));
            }
        }
    }

    [Fact(DisplayName = "Required嵌套回滚: 内层回滚后整个ambient事务回滚, 无数据持久化")]
    public async Task Required_RollbackInInner_CancelsAmbientAndNoRowsPersisted()
    {
        var scope = _sp.CreateScope();
        var sp = scope.ServiceProvider;
        using (sp.InitLifecycle())
        {
            using (sp.GetService<IDbContext>().CreateDatasourceScope())
            {
                var manager = sp.GetRequiredService<IDatasourceManager>();
                await PrepareTableAsync(manager);

                using (var outer = manager.CreateTransScope(Propagation.Required))
                {
                    await outer.BeginAsync();
                    var outerClient = manager.GetCurrentClient<ISqlSugarClient>();
                    await outerClient!.Ado.ExecuteCommandAsync("INSERT INTO tx_test (name) VALUES (@name)", new { name = "outer" });

                    using (var inner = manager.CreateTransScope(Propagation.Required))
                    {
                        await inner.BeginAsync();
                        var innerClient = manager.GetCurrentClient<ISqlSugarClient>();
                        await innerClient!.Ado.ExecuteCommandAsync("INSERT INTO tx_test (name) VALUES (@name)", new { name = "inner" });
                        await inner.RollbackAsync();
                    }

                    await outer.RollbackAsync();
                }

                Assert.Equal(0, await CountAsync(manager, "outer"));
                Assert.Equal(0, await CountAsync(manager, "inner"));
            }
        }
    }

    [Fact(DisplayName = "RequiresNew独立提交: 内层独立事务提交不影响外层, 双方均持久化")]
    public async Task RequiresNew_IndependentCommit_PersistsBoth()
    {
        var scope = _sp.CreateScope();
        var sp = scope.ServiceProvider;
        using (sp.InitLifecycle())
        {
            using (sp.GetService<IDbContext>().CreateDatasourceScope())
            {
                var manager = sp.GetRequiredService<IDatasourceManager>();
                await PrepareTableAsync(manager);

                using (var outer = manager.CreateTransScope(Propagation.Required))
                {
                    await outer.BeginAsync();
                    var outerClient = manager.GetCurrentClient<ISqlSugarClient>();
                    await outerClient!.Ado.ExecuteCommandAsync("INSERT INTO tx_test (name) VALUES (@name)", new { name = "outer" });

                    using (var inner = manager.CreateTransScope(Propagation.RequiresNew))
                    {
                        await inner.BeginAsync();
                        var innerClient = manager.GetCurrentClient<ISqlSugarClient>();
                        await innerClient!.Ado.ExecuteCommandAsync("INSERT INTO tx_test (name) VALUES (@name)", new { name = "inner" });
                        await inner.CommitAsync();
                    }

                    await outer.CommitAsync();
                }

                Assert.Equal(1, await CountAsync(manager, "outer"));
                Assert.Equal(1, await CountAsync(manager, "inner"));
            }
        }
    }

    [Fact(DisplayName = "RequiresNew内层回滚: 内层独立回滚不影响外层提交")]
    public async Task RequiresNew_RollbackInner_DoesNotAffectOuter()
    {
        var scope = _sp.CreateScope();
        var sp = scope.ServiceProvider;
        using (sp.InitLifecycle())
        {
            using (sp.GetService<IDbContext>().CreateDatasourceScope())
            {
                var manager = sp.GetRequiredService<IDatasourceManager>();
                await PrepareTableAsync(manager);

                using (var outer = manager.CreateTransScope(Propagation.Required))
                {
                    await outer.BeginAsync();
                    var outerClient = manager.GetCurrentClient<ISqlSugarClient>();
                    await outerClient!.Ado.ExecuteCommandAsync("INSERT INTO tx_test (name) VALUES (@name)", new { name = "outer" });

                    using (var inner = manager.CreateTransScope(Propagation.RequiresNew))
                    {
                        await inner.BeginAsync();
                        var innerClient = manager.GetCurrentClient<ISqlSugarClient>();
                        await innerClient!.Ado.ExecuteCommandAsync("INSERT INTO tx_test (name) VALUES (@name)", new { name = "inner" });
                        await inner.RollbackAsync();
                    }

                    await outer.CommitAsync();
                }

                Assert.Equal(1, await CountAsync(manager, "outer"));
                Assert.Equal(0, await CountAsync(manager, "inner"));
            }
        }
    }

    [Fact(DisplayName = "Mandatory无ambient: 无外层事务时CreateTransScope抛出InvalidOperationException")]
    public async Task Mandatory_WithoutAmbient_ThrowsInvalidOperationException()
    {
        var scope = _sp.CreateScope();
        var sp = scope.ServiceProvider;
        using (sp.InitLifecycle())
        {
            using (sp.GetService<IDbContext>().CreateDatasourceScope())
            {
                var manager = sp.GetRequiredService<IDatasourceManager>();
                await PrepareTableAsync(manager);
                Assert.Throws<InvalidOperationException>(() => manager.CreateTransScope(Propagation.Mandatory));
            }
        }
    }

    [Fact(DisplayName = "Mandatory嵌套: 加入外层事务, 外层提交后内外数据均持久化")]
    public async Task Mandatory_JoinsAmbientAndCommitByOuter_PersistsAllInnerAndOuterInserts()
    {
        var scope = _sp.CreateScope();
        var sp = scope.ServiceProvider;
        using (sp.InitLifecycle())
        {
            using (sp.GetService<IDbContext>().CreateDatasourceScope())
            {
                var manager = sp.GetRequiredService<IDatasourceManager>();
                await PrepareTableAsync(manager);

                using (var outer = manager.CreateTransScope(Propagation.Required))
                {
                    await outer.BeginAsync();
                    var outerClient = manager.GetCurrentClient<ISqlSugarClient>();
                    await outerClient!.Ado.ExecuteCommandAsync("INSERT INTO tx_test (name) VALUES (@name)", new { name = "outer_m" });

                    using (var inner = manager.CreateTransScope(Propagation.Mandatory))
                    {
                        await inner.BeginAsync();
                        var innerClient = manager.GetCurrentClient<ISqlSugarClient>();
                        await innerClient!.Ado.ExecuteCommandAsync("INSERT INTO tx_test (name) VALUES (@name)", new { name = "inner_m" });
                        await inner.CommitAsync();
                    }

                    await outer.CommitAsync();
                }

                Assert.Equal(1, await CountAsync(manager, "outer_m"));
                Assert.Equal(1, await CountAsync(manager, "inner_m"));
            }
        }
    }

    [Fact(DisplayName = "Mandatory内层回滚: 回滚ambient事务, 内外均无数据持久化")]
    public async Task Mandatory_InnerThrows_OuterDoesNotCatch_RollbackAll()
    {
        var scope = _sp.CreateScope();
        var sp = scope.ServiceProvider;
        using (sp.InitLifecycle())
        {
            using (sp.GetService<IDbContext>().CreateDatasourceScope())
            {
                var manager = sp.GetRequiredService<IDatasourceManager>();
                await PrepareTableAsync(manager);

                using (var outer = manager.CreateTransScope(Propagation.Required))
                {
                    await outer.BeginAsync();
                    var outerClient = manager.GetCurrentClient<ISqlSugarClient>();
                    await outerClient!.Ado.ExecuteCommandAsync("INSERT INTO tx_test (name) VALUES (@name)", new { name = "outer_m2" });

                    using (var inner = manager.CreateTransScope(Propagation.Mandatory))
                    {
                        await inner.BeginAsync();
                        var innerClient = manager.GetCurrentClient<ISqlSugarClient>();
                        await innerClient!.Ado.ExecuteCommandAsync("INSERT INTO tx_test (name) VALUES (@name)", new { name = "inner_m2" });
                        await inner.RollbackAsync();
                    }

                    await outer.RollbackAsync();
                }

                Assert.Equal(0, await CountAsync(manager, "outer_m2"));
                Assert.Equal(0, await CountAsync(manager, "inner_m2"));
            }
        }
    }

    [Fact(DisplayName = "Nested无ambient: 等价于Required, 新建事务并提交持久化")]
    public async Task Nested_WithoutAmbient_BehavesLikeRequired()
    {
        var scope = _sp.CreateScope();
        var sp = scope.ServiceProvider;
        using (sp.InitLifecycle())
        {
            using (sp.GetService<IDbContext>().CreateDatasourceScope())
            {
                var manager = sp.GetRequiredService<IDatasourceManager>();
                await PrepareTableAsync(manager);

                using (var scopeTrans = manager.CreateTransScope(Propagation.Nested))
                {
                    await scopeTrans.BeginAsync();
                    var client = manager.GetCurrentClient<ISqlSugarClient>();
                    await client!.Ado.ExecuteCommandAsync("INSERT INTO tx_test (name) VALUES (@name)", new { name = "nested_no_ambient" });
                    await scopeTrans.CommitAsync();
                }

                Assert.Equal(1, await CountAsync(manager, "nested_no_ambient"));
            }
        }
    }

    [Fact(DisplayName = "Nested嵌套: 创建savepoint, 外层提交后内外数据均持久化")]
    public async Task Nested_JoinsAmbientAndCommitByOuter_PersistsAllInnerAndOuterInserts()
    {
        var scope = _sp.CreateScope();
        var sp = scope.ServiceProvider;
        using (sp.InitLifecycle())
        {
            using (sp.GetService<IDbContext>().CreateDatasourceScope())
            {
                var manager = sp.GetRequiredService<IDatasourceManager>();
                await PrepareTableAsync(manager);

                using (var outer = manager.CreateTransScope(Propagation.Required))
                {
                    await outer.BeginAsync();
                    var outerClient = manager.GetCurrentClient<ISqlSugarClient>();
                    await outerClient!.Ado.ExecuteCommandAsync("INSERT INTO tx_test (name) VALUES (@name)", new { name = "outer_nested" });

                    using (var inner = manager.CreateTransScope(Propagation.Nested))
                    {
                        await inner.BeginAsync();
                        var innerClient = manager.GetCurrentClient<ISqlSugarClient>();
                        await innerClient!.Ado.ExecuteCommandAsync("INSERT INTO tx_test (name) VALUES (@name)", new { name = "inner_nested" });
                        await inner.CommitAsync();
                    }

                    await outer.CommitAsync();
                }

                Assert.Equal(1, await CountAsync(manager, "outer_nested"));
                Assert.Equal(1, await CountAsync(manager, "inner_nested"));
            }
        }
    }

    [Fact(DisplayName = "Nested内层回滚: 回滚到savepoint仅撤销内层, 外层仍可提交")]
    public async Task Nested_InnerRollback_RollsBackToSavepoint_OuterCanCommitOnlyOuterPersists()
    {
        var scope = _sp.CreateScope();
        var sp = scope.ServiceProvider;
        using (sp.InitLifecycle())
        {
            using (sp.GetService<IDbContext>().CreateDatasourceScope())
            {
                var manager = sp.GetRequiredService<IDatasourceManager>();
                await PrepareTableAsync(manager);

                using (var outer = manager.CreateTransScope(Propagation.Required))
                {
                    await outer.BeginAsync();
                    var outerClient = manager.GetCurrentClient<ISqlSugarClient>();
                    await outerClient!.Ado.ExecuteCommandAsync("INSERT INTO tx_test (name) VALUES (@name)", new { name = "outer_nested2" });

                    using (var inner = manager.CreateTransScope(Propagation.Nested))
                    {
                        await inner.BeginAsync();
                        var innerClient = manager.GetCurrentClient<ISqlSugarClient>();
                        await innerClient!.Ado.ExecuteCommandAsync("INSERT INTO tx_test (name) VALUES (@name)", new { name = "inner_nested2" });
                        await inner.RollbackAsync();
                    }

                    await outer.CommitAsync();
                }

                Assert.Equal(1, await CountAsync(manager, "outer_nested2"));
                Assert.Equal(0, await CountAsync(manager, "inner_nested2"));
            }
        }
    }

    [Fact(DisplayName = "Never无ambient: 以非事务方式执行并成功持久化")]
    public async Task Never_WithoutAmbient_ExecutesWithoutTransaction()
    {
        var scope = _sp.CreateScope();
        var sp = scope.ServiceProvider;
        using (sp.InitLifecycle())
        {
            using (sp.GetService<IDbContext>().CreateDatasourceScope())
            {
                var manager = sp.GetRequiredService<IDatasourceManager>();
                await PrepareTableAsync(manager);

                using (var scopeTrans = manager.CreateTransScope(Propagation.Never))
                {
                    await scopeTrans.BeginAsync();
                    var client = manager.GetCurrentClient<ISqlSugarClient>();
                    await client!.Ado.ExecuteCommandAsync("INSERT INTO tx_test (name) VALUES (@name)", new { name = "never_mgr" });
                    await scopeTrans.CommitAsync();
                }

                Assert.Equal(1, await CountAsync(manager, "never_mgr"));
            }
        }
    }

    [Fact(DisplayName = "Never有ambient: 有外层事务时CreateTransScope抛出InvalidOperationException")]
    public async Task Never_WithAmbient_ThrowsInvalidOperationException()
    {
        var scope = _sp.CreateScope();
        var sp = scope.ServiceProvider;
        using (sp.InitLifecycle())
        {
            using (sp.GetService<IDbContext>().CreateDatasourceScope())
            {
                var manager = sp.GetRequiredService<IDatasourceManager>();
                await PrepareTableAsync(manager);

                using (var outer = manager.CreateTransScope(Propagation.Required))
                {
                    await outer.BeginAsync();
                    Assert.Throws<InvalidOperationException>(() => manager.CreateTransScope(Propagation.Never));
                    await outer.RollbackAsync();
                }
            }
        }
    }

    [Fact(DisplayName = "事务回调: 无事务时立即执行")]
    public async Task TransactionCallback_WithoutAmbient_ExecutesImmediately()
    {
        var scope = _sp.CreateScope();
        var sp = scope.ServiceProvider;
        using (sp.InitLifecycle())
        {
            var callbacks = new List<string>();
            var registry = sp.GetRequiredService<ITransactionCallbackRegistry>();

            await registry.RegisterAfterCommitAsync(() =>
            {
                callbacks.Add("immediate");
                return Task.CompletedTask;
            });

            Assert.Equal(["immediate"], callbacks);
        }
    }

    [Fact(DisplayName = "事务回调: Required根事务提交后执行, 回滚后不执行")]
    public async Task TransactionCallback_RequiredCommitAndRollback_ExecutesOnlyAfterCommit()
    {
        var scope = _sp.CreateScope();
        var sp = scope.ServiceProvider;
        using (sp.InitLifecycle())
        {
            using (sp.GetService<IDbContext>().CreateDatasourceScope())
            {
                var callbacks = new List<string>();
                var manager = sp.GetRequiredService<IDatasourceManager>();
                var registry = sp.GetRequiredService<ITransactionCallbackRegistry>();

                using (var transaction = manager.CreateTransScope(Propagation.Required))
                {
                    await transaction.BeginAsync();
                    await registry.RegisterAfterCommitAsync(() =>
                    {
                        callbacks.Add("commit");
                        return Task.CompletedTask;
                    });

                    Assert.Empty(callbacks);
                    await transaction.CommitAsync();
                }

                using (var transaction = manager.CreateTransScope(Propagation.Required))
                {
                    await transaction.BeginAsync();
                    await registry.RegisterAfterCommitAsync(() =>
                    {
                        callbacks.Add("rollback");
                        return Task.CompletedTask;
                    });

                    await transaction.RollbackAsync();
                }

                Assert.Equal(["commit"], callbacks);
            }
        }
    }

    [Fact(DisplayName = "事务回调: Mandatory加入外层事务, 只在外层提交后执行")]
    public async Task TransactionCallback_Mandatory_ExecutesOnlyWhenOuterCommits()
    {
        var scope = _sp.CreateScope();
        var sp = scope.ServiceProvider;
        using (sp.InitLifecycle())
        {
            using (sp.GetService<IDbContext>().CreateDatasourceScope())
            {
                var callbacks = new List<string>();
                var manager = sp.GetRequiredService<IDatasourceManager>();
                var registry = sp.GetRequiredService<ITransactionCallbackRegistry>();

                using (var outer = manager.CreateTransScope(Propagation.Required))
                {
                    await outer.BeginAsync();
                    using (var inner = manager.CreateTransScope(Propagation.Mandatory))
                    {
                        await inner.BeginAsync();
                        await registry.RegisterAfterCommitAsync(() =>
                        {
                            callbacks.Add("mandatory");
                            return Task.CompletedTask;
                        });
                        await inner.CommitAsync();
                    }

                    Assert.Empty(callbacks);
                    await outer.CommitAsync();
                }

                Assert.Equal(["mandatory"], callbacks);
            }
        }
    }

    [Fact(DisplayName = "事务回调: RequiresNew独立提交后执行, 不受外层回滚影响")]
    public async Task TransactionCallback_RequiresNew_ExecutesBeforeOuterRollback()
    {
        var scope = _sp.CreateScope();
        var sp = scope.ServiceProvider;
        using (sp.InitLifecycle())
        {
            using (sp.GetService<IDbContext>().CreateDatasourceScope())
            {
                var callbacks = new List<string>();
                var manager = sp.GetRequiredService<IDatasourceManager>();
                var registry = sp.GetRequiredService<ITransactionCallbackRegistry>();

                using (var outer = manager.CreateTransScope(Propagation.Required))
                {
                    await outer.BeginAsync();
                    await registry.RegisterAfterCommitAsync(() =>
                    {
                        callbacks.Add("outer");
                        return Task.CompletedTask;
                    });

                    using (var inner = manager.CreateTransScope(Propagation.RequiresNew))
                    {
                        await inner.BeginAsync();
                        await registry.RegisterAfterCommitAsync(() =>
                        {
                            callbacks.Add("inner");
                            return Task.CompletedTask;
                        });
                        await inner.CommitAsync();
                    }

                    Assert.Equal(["inner"], callbacks);
                    await outer.RollbackAsync();
                }

                Assert.Equal(["inner"], callbacks);
            }
        }
    }

    [Fact(DisplayName = "事务回调: Nested回滚丢弃内层回调, Nested提交后随外层提交执行")]
    public async Task TransactionCallback_Nested_HandlesSavepointCommitAndRollback()
    {
        var scope = _sp.CreateScope();
        var sp = scope.ServiceProvider;
        using (sp.InitLifecycle())
        {
            using (sp.GetService<IDbContext>().CreateDatasourceScope())
            {
                var callbacks = new List<string>();
                var manager = sp.GetRequiredService<IDatasourceManager>();
                var registry = sp.GetRequiredService<ITransactionCallbackRegistry>();

                using (var outer = manager.CreateTransScope(Propagation.Required))
                {
                    await outer.BeginAsync();
                    using (var inner = manager.CreateTransScope(Propagation.Nested))
                    {
                        await inner.BeginAsync();
                        await registry.RegisterAfterCommitAsync(() =>
                        {
                            callbacks.Add("discarded");
                            return Task.CompletedTask;
                        });
                        await inner.RollbackAsync();
                    }

                    using (var inner = manager.CreateTransScope(Propagation.Nested))
                    {
                        await inner.BeginAsync();
                        await registry.RegisterAfterCommitAsync(() =>
                        {
                            callbacks.Add("nested");
                            return Task.CompletedTask;
                        });
                        await inner.CommitAsync();
                    }

                    Assert.Empty(callbacks);
                    await outer.CommitAsync();
                }

                Assert.Equal(["nested"], callbacks);
            }
        }
    }

    [Fact(DisplayName = "事务回调: 回调异常不影响提交, 后续回调继续执行")]
    public async Task TransactionCallback_CallbackFailure_DoesNotThrowAndContinues()
    {
        var scope = _sp.CreateScope();
        var sp = scope.ServiceProvider;
        using (sp.InitLifecycle())
        {
            using (sp.GetService<IDbContext>().CreateDatasourceScope())
            {
                var callbacks = new List<string>();
                var manager = sp.GetRequiredService<IDatasourceManager>();
                var registry = sp.GetRequiredService<ITransactionCallbackRegistry>();

                using (var transaction = manager.CreateTransScope(Propagation.Required))
                {
                    await transaction.BeginAsync();
                    await registry.RegisterAfterCommitAsync(() => throw new InvalidOperationException("callback failed"));
                    await registry.RegisterAfterCommitAsync(() =>
                    {
                        callbacks.Add("continued");
                        return Task.CompletedTask;
                    });

                    await transaction.CommitAsync();
                }

                Assert.Equal(["continued"], callbacks);
            }
        }
    }
}
