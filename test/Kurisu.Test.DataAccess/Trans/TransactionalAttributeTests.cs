
using System.Diagnostics.CodeAnalysis;
using Kurisu.AspNetCore.Abstractions.DataAccess.Core.Context;
using Kurisu.AspNetCore.Abstractions.Startup;
using Kurisu.Extensions.SqlSugar.Utils;
using Kurisu.Test.DataAccess.Trans.Mock;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Test.DataAccess.Trans;

[Trait("Db","Trans")]
public class TransactionalAttributeTests
{
    private readonly IServiceProvider _sp;

    [ExcludeFromCodeCoverage]
    public TransactionalAttributeTests()
    {
        _sp = TestHelper.GetServiceProvider();
    }

    /// <summary>
    /// 准备测试用表：若不存在则创建，并清空数据。
    /// </summary>
    private async Task PrepareTableAsync(IDbContext dbContext)
    {
        dbContext.CodeFirst.EnsureTableExists(typeof(TxTest));
        // 使用 DbContext 的 Deleteable API 清空表，而不是直接执行 SQL
        await dbContext.AsSqlSugarDbContext().Deleteable<TxTest>().ExecuteCommandAsync();
    }

    /// <summary>
    /// 统计指定 name 在测试表中的行数
    /// </summary>
    private async Task<int> CountAsync(IDbContext dbContext, string name)
    {
        var q = dbContext.Queryable<TxTest>();
        return await q.CountAsync(x => x.Name == name);
    }

    [Fact]
    public async Task Transactional_Commits_OnSuccess()
    {
        var scope = _sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                await PrepareTableAsync(ctx);

                var service = scope.ServiceProvider.GetRequiredService<ITransactionalInnerService>();
                await service.InsertAsync("commit");

                var count = await CountAsync(ctx, "commit");
                Assert.Equal(1, count);
            }
        }

    }

    [Fact]
    public async Task Transactional_Rollbacks_OnException()
    {
        var scope = _sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                await PrepareTableAsync(ctx);

                var service = scope.ServiceProvider.GetRequiredService<ITransactionalInnerService>();
                await Assert.ThrowsAsync<Exception>(async () => await service.InsertAndThrowAsync("rollback"));

                var count = await CountAsync(ctx, "rollback");
                Assert.Equal(0, count);
            }
        }

    }

    [Fact]
    public async Task Required_Propagation_Commits_All()
    {
        var scope = _sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                await PrepareTableAsync(ctx);

                var service = scope.ServiceProvider.GetRequiredService<ITransactionalOuterService>();
                await service.OuterRequiredAsync("outer", "inner");

                Assert.Equal(1, await CountAsync(ctx, "outer"));
                Assert.Equal(1, await CountAsync(ctx, "inner"));
            }
        }

    }

    [Fact]
    public async Task Required_Propagation_Rollback_All_OnException()
    {
        var scope = _sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                await PrepareTableAsync(ctx);

                var service = scope.ServiceProvider.GetRequiredService<ITransactionalOuterService>();
                await Assert.ThrowsAsync<Exception>(async () => await service.OuterRequiredOnExceptionAsync("outer", "inner"));

                Assert.Equal(0, await CountAsync(ctx, "outer"));
                Assert.Equal(0, await CountAsync(ctx, "inner"));
            }
        }

    }

    [Fact]
    public async Task RequiresNew_Propagation_OuterCommit_InnerRollback()
    {
        var scope = _sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                await PrepareTableAsync(ctx);

                var service = scope.ServiceProvider.GetRequiredService<ITransactionalOuterService>();
                await service.OuterRequiresNewRollbackAsync("outer", "inner");

                Assert.Equal(1, await CountAsync(ctx, "outer"));
                Assert.Equal(0, await CountAsync(ctx, "inner"));
            }
        }

    }

    [Fact]
    public async Task RequiresNew_Propagation_OuterCommit_InnerCommit()
    {
        var scope = _sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                await PrepareTableAsync(ctx);

                var service = scope.ServiceProvider.GetRequiredService<ITransactionalOuterService>();
                await service.OuterRequiresNewAsync("outer", "inner");

                Assert.Equal(1, await CountAsync(ctx, "outer"));
                Assert.Equal(1, await CountAsync(ctx, "inner"));
            }
        }

    }

    [Fact]
    public async Task RequiresNew_Propagation_OuterRollback_WhenInnerThrows_Uncaught()
    {
        var scope = _sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                await PrepareTableAsync(ctx);

                var service = scope.ServiceProvider.GetRequiredService<ITransactionalOuterService>();
                await Assert.ThrowsAsync<Exception>(async () => await service.OuterRequiresNewNoCatchAsync("outer", "inner"));

                // inner is rolled back (RequiresNew), and because outer did not catch the exception, outer is rolled back too
                Assert.Equal(0, await CountAsync(ctx, "outer"));
                Assert.Equal(0, await CountAsync(ctx, "inner"));
            }
        }

    }

    [Fact]
    public async Task Required_Propagation_InnerThrows_OuterCatches_RollbackAll()
    {
        var scope = _sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                await PrepareTableAsync(ctx);

                var service = scope.ServiceProvider.GetRequiredService<ITransactionalOuterService>();
                // outer catches inner's exception; with Required propagation the inner shares the transaction,
                // and because the exception is caught, the transaction is not aborted and will commit.
                await service.OuterRequiredInnerThrowsCatchAsync("outer", "inner");

                Assert.Equal(1, await CountAsync(ctx, "outer"));
                Assert.Equal(1, await CountAsync(ctx, "inner"));
            }
        }
    }

    [Fact]
    public async Task Required_Propagation_InnerThrows_OuterDoesNotCatch_RollbackAll()
    {
        var scope = _sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                await PrepareTableAsync(ctx);

                var service = scope.ServiceProvider.GetRequiredService<ITransactionalOuterService>();
                await Assert.ThrowsAsync<Exception>(async () => await service.OuterRequiredInnerThrowsNoCatchAsync("outer", "inner"));

                Assert.Equal(0, await CountAsync(ctx, "outer"));
                Assert.Equal(0, await CountAsync(ctx, "inner"));
            }
        }
    }

    [Fact]
    public async Task RequiresNew_OuterInnerRequired_InnerThrows_OuterCatches_RollbackAll()
    {
        var scope = _sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                await PrepareTableAsync(ctx);

                var service = scope.ServiceProvider.GetRequiredService<ITransactionalOuterService>();
                // outer is RequiresNew, inner uses Required (joins outer transaction) and throws; outer catches the exception,
                // so the transaction is not aborted and outer will commit (both inserts persist).
                await service.OuterRequiresNewInnerThrowsCatchAsync("outer", "inner");

                Assert.Equal(1, await CountAsync(ctx, "outer"));
                Assert.Equal(1, await CountAsync(ctx, "inner"));
            }
        }
    }

    [Fact]
    public async Task RequiresNew_OuterInnerRequired_InnerThrows_OuterDoesNotCatch_RollbackAll()
    {
        var scope = _sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                await PrepareTableAsync(ctx);

                var service = scope.ServiceProvider.GetRequiredService<ITransactionalOuterService>();
                await Assert.ThrowsAsync<Exception>(async () => await service.OuterRequiresNewInnerThrowsNoCatchAsync("outer", "inner"));

                Assert.Equal(0, await CountAsync(ctx, "outer"));
                Assert.Equal(0, await CountAsync(ctx, "inner"));
            }
        }
    }

    [Fact]
    public async Task Transactional_NoRollbackFor_Commits_WhenSpecifiedException()
    {
        var scope = _sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                await PrepareTableAsync(ctx);

                var service = scope.ServiceProvider.GetRequiredService<ITransactionalInnerService>();
                // The Transactional attribute is configured with NoRollbackFor = TestNotRollbackException,
                // the interceptor will call CommitAsync for that exception but rethrow it, so the caller sees the exception.
                await Assert.ThrowsAsync<TestNotRollbackException>(async () => await service.InsertAndThrowNoRollbackAsync("nrb"));

                // Even though the exception is rethrown, the inner transaction (since this is a direct call) should have been committed.
                Assert.Equal(1, await CountAsync(ctx, "nrb"));
            }
        }
    }

    [Fact]
    public async Task OuterRequiredInnerNoRollback_CommitsBoth_WhenInnerMarkedNoRollbackFor()
    {
        var scope = _sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                await PrepareTableAsync(ctx);

                var service = scope.ServiceProvider.GetRequiredService<ITransactionalOuterService>();
                // inner has NoRollbackFor but uses REQUIRED (joins outer). The interceptor will call CommitAsync for the inner exception
                // but will rethrow; because outer does not catch the exception, outer will roll back the shared transaction.
                await Assert.ThrowsAsync<TestNotRollbackException>(async () => await service.OuterRequiredInnerNoRollbackAsync("outer", "inner"));

                // Outer rollback causes both inserts to be rolled back.
                Assert.Equal(0, await CountAsync(ctx, "outer"));
                Assert.Equal(0, await CountAsync(ctx, "inner"));
            }
        }

    }

    [Fact]
    public async Task OuterRequiredInnerSwallow_CommitsBoth_WhenInnerSwallowsException()
    {
        var scope = _sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                await PrepareTableAsync(ctx);

                var service = scope.ServiceProvider.GetRequiredService<ITransactionalOuterService>();
                // inner swallows the TestNotRollbackException explicitly; outer should not see an exception and will commit
                await service.OuterRequiredInnerSwallowAsync("outer", "inner");

                Assert.Equal(1, await CountAsync(ctx, "outer"));
                Assert.Equal(1, await CountAsync(ctx, "inner"));
            }
        }

    }

    [Fact]
    public async Task OuterRequiredInnerRequiresNew_NoRollback_CommitsInnerAndOuter()
    {
        var scope = _sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                await PrepareTableAsync(ctx);

                var service = scope.ServiceProvider.GetRequiredService<ITransactionalOuterService>();
                // inner uses REQUIRES_NEW with NoRollbackFor: it will commit its own transaction but rethrow the exception.
                // Since outer does not catch the exception, outer's transaction will be rolled back.
                await Assert.ThrowsAsync<TestNotRollbackException>(async () => await service.OuterRequiredInnerRequiresNewNoCatchAsync("outer", "inner"));

                // inner committed (RequiresNew), outer rolled back
                Assert.Equal(0, await CountAsync(ctx, "outer"));
                Assert.Equal(1, await CountAsync(ctx, "inner"));
            }
        }
    }

    [Fact]
    public async Task Mandatory_WithoutAmbient_ThrowsInvalidOperationException()
    {
        var scope = _sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                var service = scope.ServiceProvider.GetRequiredService<ITransactionalInnerService>();
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await service.InnerMandatoryAsync("m1"));
            }
        }
    }

    [Fact]
    public async Task Mandatory_WithAmbient_CommitsBoth()
    {
        var scope = _sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                await PrepareTableAsync(ctx);

                var service = scope.ServiceProvider.GetRequiredService<ITransactionalOuterService>();
                await service.OuterRequiredCallsMandatoryAsync("outer_m", "inner_m");

                Assert.Equal(1, await CountAsync(ctx, "outer_m"));
                Assert.Equal(1, await CountAsync(ctx, "inner_m"));
            }
        }
    }

    [Fact]
    public async Task Mandatory_WithAmbient_InnerThrows_OuterDoesNotCatch_RollbackAll()
    {
        var scope = _sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                await PrepareTableAsync(ctx);

                var service = scope.ServiceProvider.GetRequiredService<ITransactionalOuterService>();
                await Assert.ThrowsAsync<Exception>(async () => await service.OuterRequiredCallsMandatoryAndThrowNoCatchAsync("outer_m2", "inner_m2"));

                Assert.Equal(0, await CountAsync(ctx, "outer_m2"));
                Assert.Equal(0, await CountAsync(ctx, "inner_m2"));
            }
        }
    }

    [Fact]
    public async Task Mandatory_WithAmbient_InnerThrows_OuterCatches_CommitsBoth()
    {
        var scope = _sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                await PrepareTableAsync(ctx);

                var service = scope.ServiceProvider.GetRequiredService<ITransactionalOuterService>();
                await service.OuterRequiredCallsMandatoryAndThrowCatchAsync("outer_m3", "inner_m3");

                Assert.Equal(1, await CountAsync(ctx, "outer_m3"));
                Assert.Equal(1, await CountAsync(ctx, "inner_m3"));
            }
        }
    }

    [Fact]
    public async Task Nested_WithAmbient_CommitsInnerRolledUpToOuterWhenNoErrors()
    {
        var scope = _sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                await PrepareTableAsync(ctx);

                var service = scope.ServiceProvider.GetRequiredService<ITransactionalOuterService>();
                await service.OuterRequiredCallsNestedAsync("outer_n", "inner_n");

                Assert.Equal(1, await CountAsync(ctx, "outer_n"));
                Assert.Equal(1, await CountAsync(ctx, "inner_n"));
            }
        }

    }

    [Fact]
    public async Task Nested_WithAmbient_InnerThrows_OuterDoesNotCatch_RollbackAll()
    {
        var scope = _sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                await PrepareTableAsync(ctx);

                var service = scope.ServiceProvider.GetRequiredService<ITransactionalOuterService>();
                await Assert.ThrowsAsync<Exception>(async () => await service.OuterRequiredCallsNestedAndThrowNoCatchAsync("outer_n2", "inner_n2"));

                Assert.Equal(0, await CountAsync(ctx, "outer_n2"));
                Assert.Equal(0, await CountAsync(ctx, "inner_n2"));
            }
        }

    }

    [Fact]
    public async Task Nested_WithAmbient_InnerThrows_OuterCatches_OnlyOuterPersists()
    {
        var scope = _sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                await PrepareTableAsync(ctx);

                var service = scope.ServiceProvider.GetRequiredService<ITransactionalOuterService>();
                await service.OuterRequiredCallsNestedAndThrowCatchAsync("outer_n3", "inner_n3");

                Assert.Equal(1, await CountAsync(ctx, "outer_n3"));
                Assert.Equal(0, await CountAsync(ctx, "inner_n3"));
            }
        }
    }

    [Fact]
    public async Task Never_WithoutAmbient_Commits()
    {
        var scope = _sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                await PrepareTableAsync(ctx);

                var service = scope.ServiceProvider.GetRequiredService<ITransactionalInnerService>();
                await service.InnerNeverAsync("never1");

                Assert.Equal(1, await CountAsync(ctx, "never1"));
            }
        }
    }

    [Fact]
    public async Task Never_WithAmbient_ThrowsInvalidOperationException()
    {
        var scope = _sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                await PrepareTableAsync(ctx);

                var service = scope.ServiceProvider.GetRequiredService<ITransactionalOuterService>();
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await service.OuterRequiredCallsNeverAsync("outer_never", "inner_never"));

                Assert.Equal(0, await CountAsync(ctx, "outer_never"));
                Assert.Equal(0, await CountAsync(ctx, "inner_never"));
            }
        }
    }
}