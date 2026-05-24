using System.Diagnostics.CodeAnalysis;
using Kurisu.AspNetCore.Abstractions.DataAccess.Core.Context;
using Kurisu.AspNetCore.Abstractions.Startup;
using Kurisu.Extensions.SqlSugar.Utils;
using Kurisu.Test.DataAccess.Trans.Mock;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Test.DataAccess.Trans;

[Trait("Db", "Trans")]
public class TransactionalAttributeTests
{
    private readonly IServiceProvider _sp;

    [ExcludeFromCodeCoverage]
    public TransactionalAttributeTests()
    {
        _sp = TestHelper.GetServiceProvider();
    }

    private async Task PrepareTableAsync(IDbContext dbContext)
    {
        dbContext.CodeFirst.EnsureTableExists(typeof(TxTest));
        await dbContext.AsSqlSugarDbContext().Deleteable<TxTest>().ExecuteCommandAsync();
    }

    private async Task<int> CountAsync(IDbContext dbContext, string name)
    {
        return await dbContext.Queryable<TxTest>().CountAsync(x => x.Name == name);
    }

    [Fact(DisplayName = "Transactional成功提交: 方法正常返回后数据持久化")]
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
                Assert.Equal(1, await CountAsync(ctx, "commit"));
            }
        }
    }

    [Fact(DisplayName = "Transactional异常回滚: 方法抛异常后数据未持久化")]
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
                Assert.Equal(0, await CountAsync(ctx, "rollback"));
            }
        }
    }

    [Fact(DisplayName = "Required传播成功: 外层内层均成功, 双方数据均持久化")]
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

    [Fact(DisplayName = "Required传播异常: 内层异常导致ambient事务回滚, 双方数据均不可见")]
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

    [Fact(DisplayName = "RequiresNew内层回滚: 内层独立事务回滚, 外层事务提交成功")]
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

    [Fact(DisplayName = "RequiresNew双方提交: 内外独立事务均提交成功")]
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

    [Fact(DisplayName = "RequiresNew内层异常未捕获: 内层异常导致外层也回滚")]
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
                Assert.Equal(0, await CountAsync(ctx, "outer"));
                Assert.Equal(0, await CountAsync(ctx, "inner"));
            }
        }
    }

    [Fact(DisplayName = "Required内层异常外层捕获: 外层catch后事务正常提交, 双方数据持久化")]
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
                await service.OuterRequiredInnerThrowsCatchAsync("outer", "inner");
                Assert.Equal(1, await CountAsync(ctx, "outer"));
                Assert.Equal(1, await CountAsync(ctx, "inner"));
            }
        }
    }

    [Fact(DisplayName = "Required内层异常外层未捕获: 异常传播导致ambient事务回滚")]
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

    [Fact(DisplayName = "RequiresNew外层Required内层异常外层捕获: 捕获后双方提交成功")]
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
                await service.OuterRequiresNewInnerThrowsCatchAsync("outer", "inner");
                Assert.Equal(1, await CountAsync(ctx, "outer"));
                Assert.Equal(1, await CountAsync(ctx, "inner"));
            }
        }
    }

    [Fact(DisplayName = "RequiresNew外层Required内层异常未捕获: 异常传播双方回滚")]
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

    [Fact(DisplayName = "NoRollbackFor匹配异常: 事务提交成功且异常重新抛出, 数据持久化")]
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
                await Assert.ThrowsAsync<TestNotRollbackException>(async () => await service.InsertAndThrowNoRollbackAsync("nrb"));
                Assert.Equal(1, await CountAsync(ctx, "nrb"));
            }
        }
    }

    [Fact(DisplayName = "NoRollbackFor内层Required: 内层标记NoRollback但外层未捕获, 共享事务回滚")]
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
                await Assert.ThrowsAsync<TestNotRollbackException>(async () => await service.OuterRequiredInnerNoRollbackAsync("outer", "inner"));
                Assert.Equal(0, await CountAsync(ctx, "outer"));
                Assert.Equal(0, await CountAsync(ctx, "inner"));
            }
        }
    }

    [Fact(DisplayName = "内层吞异常: 内层捕获异常后外层正常提交, 双方数据持久化")]
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
                await service.OuterRequiredInnerSwallowAsync("outer", "inner");
                Assert.Equal(1, await CountAsync(ctx, "outer"));
                Assert.Equal(1, await CountAsync(ctx, "inner"));
            }
        }
    }

    [Fact(DisplayName = "NoRollbackFor内层RequiresNew: 内层独立提交成功, 外层异常回滚, 仅内层数据持久化")]
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
                await Assert.ThrowsAsync<TestNotRollbackException>(async () => await service.OuterRequiredInnerRequiresNewNoCatchAsync("outer", "inner"));
                Assert.Equal(0, await CountAsync(ctx, "outer"));
                Assert.Equal(1, await CountAsync(ctx, "inner"));
            }
        }
    }

    [Fact(DisplayName = "Mandatory无ambient: 独立调用时CreateTransScope抛出InvalidOperationException")]
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

    [Fact(DisplayName = "Mandatory有ambient: 加入外层事务提交, 双方数据持久化")]
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

    [Fact(DisplayName = "Mandatory内层异常未捕获: 异常传播导致ambient事务回滚")]
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

    [Fact(DisplayName = "Mandatory内层异常外层捕获: 捕获后共享事务提交, 双方数据持久化")]
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

    [Fact(DisplayName = "Nested有ambient: 创建savepoint, 外层提交后内外数据均持久化")]
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

    [Fact(DisplayName = "Nested内层异常未捕获: 异常传播导致ambient事务回滚")]
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

    [Fact(DisplayName = "Nested内层异常外层捕获: 回滚到savepoint仅撤销内层, 外层可提交")]
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

    [Fact(DisplayName = "Never无ambient: 以非事务方式提交成功")]
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

    [Fact(DisplayName = "Never有ambient: 有外层事务时抛出InvalidOperationException")]
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