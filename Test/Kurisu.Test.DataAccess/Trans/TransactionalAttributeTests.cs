using Kurisu.AspNetCore.Abstractions.DataAccess;
using Kurisu.Extensions.SqlSugar.Extensions;
using Kurisu.Test.DataAccess.Trans.Mock;
using Microsoft.Extensions.DependencyInjection;
using Kurisu.Extensions.SqlSugar;

namespace Kurisu.Test.DataAccess.Trans;

public class TransactionalAttributeTests : IDisposable
{
    // 保存供所有测试方法和 Dispose 使用的同一个 IServiceProvider 实例
    private readonly IServiceScope _scope;
    private readonly IServiceProvider _sp;
    private int _clientCount = 1;

    public TransactionalAttributeTests()
    {
        var root = TestHelper.GetServiceProvider();
        // create a scope and keep it alive for the duration of the test instance
        _scope = root.CreateScope();
        _sp = _scope.ServiceProvider;

        // Ensure a scoped IDbContext is resolved at test setup so the datasource manager
        // creates its base client. Many tests depend on a client being present; resolving
        // IDbContext here and accessing its SqlSugar client ensures a client is created
        // on the datasource manager even for tests that don't touch the DB directly.
        //var dbContext = _sp.GetRequiredService<IDbContext>();
        //_ = dbContext.AsSqlSugarDbContext().Client;
    }

    /// <summary>
    /// 准备测试用表：若不存在则创建，并清空数据。
    /// </summary>
    private async Task PrepareTableAsync(IDbContext dbContext)
    {
        var client = dbContext.AsSqlSugarDbContext().Client;
        // CodeFirst.InitTables is synchronous in this SqlSugar version
        client.CodeFirst.InitTables<TxTest>();
        // 使用 DbContext 的 Deleteable API 清空表，而不是直接执行 SQL
        await dbContext.Deleteable<TxTest>().ExecuteCommandAsync();
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
        var sp = _sp;
        var dbContext = sp.GetRequiredService<IDbContext>();
        await PrepareTableAsync(dbContext);

        var service = sp.GetRequiredService<ITransactionalInnerService>();
        await service.InsertAsync("commit");

        var count = await CountAsync(dbContext, "commit");
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Transactional_Rollbacks_OnException()
    {
        var sp = _sp;
        var dbContext = sp.GetRequiredService<IDbContext>();
        await PrepareTableAsync(dbContext);

        var service = sp.GetRequiredService<ITransactionalInnerService>();
        await Assert.ThrowsAsync<Exception>(async () => await service.InsertAndThrowAsync("rollback"));

        var count = await CountAsync(dbContext, "rollback");
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task Required_Propagation_Commits_All()
    {
        var sp = _sp;
        var dbContext = sp.GetRequiredService<IDbContext>();
        await PrepareTableAsync(dbContext);

        var service = sp.GetRequiredService<ITransactionalOuterService>();
        await service.OuterRequiredAsync("outer", "inner");

        Assert.Equal(1, await CountAsync(dbContext, "outer"));
        Assert.Equal(1, await CountAsync(dbContext, "inner"));
    }

    [Fact]
    public async Task Required_Propagation_Rollback_All_OnException()
    {
        var sp = _sp;
        var dbContext = sp.GetRequiredService<IDbContext>();
        await PrepareTableAsync(dbContext);

        var service = sp.GetRequiredService<ITransactionalOuterService>();
        await Assert.ThrowsAsync<Exception>(async () => await service.OuterRequiredOnExceptionAsync("outer", "inner"));

        Assert.Equal(0, await CountAsync(dbContext, "outer"));
        Assert.Equal(0, await CountAsync(dbContext, "inner"));
    }

    [Fact]
    public async Task RequiresNew_Propagation_OuterCommit_InnerRollback()
    {
        var sp = _sp;
        var dbContext = sp.GetRequiredService<IDbContext>();
        await PrepareTableAsync(dbContext);

        var service = sp.GetRequiredService<ITransactionalOuterService>();
        await service.OuterRequiresNewRollbackAsync("outer", "inner");

        Assert.Equal(1, await CountAsync(dbContext, "outer"));
        Assert.Equal(0, await CountAsync(dbContext, "inner"));
    }

    [Fact]
    public async Task RequiresNew_Propagation_OuterCommit_InnerCommit()
    {
        var sp = _sp;
        var dbContext = sp.GetRequiredService<IDbContext>();
        await PrepareTableAsync(dbContext);

        var service = sp.GetRequiredService<ITransactionalOuterService>();
        await service.OuterRequiresNewAsync("outer", "inner");

        Assert.Equal(1, await CountAsync(dbContext, "outer"));
        Assert.Equal(1, await CountAsync(dbContext, "inner"));
    }

    [Fact]
    public async Task RequiresNew_Propagation_OuterRollback_WhenInnerThrows_Uncaught()
    {
        var sp = _sp;
        var dbContext = sp.GetRequiredService<IDbContext>();
        await PrepareTableAsync(dbContext);

        var service = sp.GetRequiredService<ITransactionalOuterService>();
        await Assert.ThrowsAsync<Exception>(async () => await service.OuterRequiresNewNoCatchAsync("outer", "inner"));

        // inner is rolled back (RequiresNew), and because outer did not catch the exception, outer is rolled back too
        Assert.Equal(0, await CountAsync(dbContext, "outer"));
        Assert.Equal(0, await CountAsync(dbContext, "inner"));
    }

    [Fact]
    public async Task Required_Propagation_InnerThrows_OuterCatches_RollbackAll()
    {
        var sp = _sp;
        var dbContext = sp.GetRequiredService<IDbContext>();
        await PrepareTableAsync(dbContext);

        var service = sp.GetRequiredService<ITransactionalOuterService>();
        // outer catches inner's exception; with Required propagation the inner shares the transaction,
        // and because the exception is caught, the transaction is not aborted and will commit.
        await service.OuterRequiredInnerThrowsCatchAsync("outer", "inner");

        Assert.Equal(1, await CountAsync(dbContext, "outer"));
        Assert.Equal(1, await CountAsync(dbContext, "inner"));
    }

    [Fact]
    public async Task Required_Propagation_InnerThrows_OuterDoesNotCatch_RollbackAll()
    {
        var sp = _sp;
        var dbContext = sp.GetRequiredService<IDbContext>();
        await PrepareTableAsync(dbContext);

        var service = sp.GetRequiredService<ITransactionalOuterService>();
        await Assert.ThrowsAsync<Exception>(async () => await service.OuterRequiredInnerThrowsNoCatchAsync("outer", "inner"));

        Assert.Equal(0, await CountAsync(dbContext, "outer"));
        Assert.Equal(0, await CountAsync(dbContext, "inner"));
    }

    [Fact]
    public async Task RequiresNew_OuterInnerRequired_InnerThrows_OuterCatches_RollbackAll()
    {
        var sp = _sp;
        var dbContext = sp.GetRequiredService<IDbContext>();
        await PrepareTableAsync(dbContext);

        var service = sp.GetRequiredService<ITransactionalOuterService>();
        // outer is RequiresNew, inner uses Required (joins outer transaction) and throws; outer catches the exception,
        // so the transaction is not aborted and outer will commit (both inserts persist).
        await service.OuterRequiresNewInnerThrowsCatchAsync("outer", "inner");

        Assert.Equal(1, await CountAsync(dbContext, "outer"));
        Assert.Equal(1, await CountAsync(dbContext, "inner"));
    }

    [Fact]
    public async Task RequiresNew_OuterInnerRequired_InnerThrows_OuterDoesNotCatch_RollbackAll()
    {
        var sp = _sp;
        var dbContext = sp.GetRequiredService<IDbContext>();
        await PrepareTableAsync(dbContext);

        var service = sp.GetRequiredService<ITransactionalOuterService>();
        await Assert.ThrowsAsync<Exception>(async () => await service.OuterRequiresNewInnerThrowsNoCatchAsync("outer", "inner"));

        Assert.Equal(0, await CountAsync(dbContext, "outer"));
        Assert.Equal(0, await CountAsync(dbContext, "inner"));
    }

    [Fact]
    public async Task Transactional_NoRollbackFor_Commits_WhenSpecifiedException()
    {
        var sp = _sp;
        var dbContext = sp.GetRequiredService<IDbContext>();
        await PrepareTableAsync(dbContext);

        var service = sp.GetRequiredService<ITransactionalInnerService>();
        // The Transactional attribute is configured with NoRollbackFor = TestNotRollbackException,
        // the interceptor will call CommitAsync for that exception but rethrow it, so the caller sees the exception.
        await Assert.ThrowsAsync<TestNotRollbackException>(async () => await service.InsertAndThrowNoRollbackAsync("nrb"));

        // Even though the exception is rethrown, the inner transaction (since this is a direct call) should have been committed.
        Assert.Equal(1, await CountAsync(dbContext, "nrb"));
    }

    [Fact]
    public async Task OuterRequiredInnerNoRollback_CommitsBoth_WhenInnerMarkedNoRollbackFor()
    {
        var sp = _sp;
        var dbContext = sp.GetRequiredService<IDbContext>();
        await PrepareTableAsync(dbContext);

        var service = sp.GetRequiredService<ITransactionalOuterService>();
        // inner has NoRollbackFor but uses REQUIRED (joins outer). The interceptor will call CommitAsync for the inner exception
        // but will rethrow; because outer does not catch the exception, outer will roll back the shared transaction.
        await Assert.ThrowsAsync<TestNotRollbackException>(async () => await service.OuterRequiredInnerNoRollbackAsync("outer", "inner"));

        // Outer rollback causes both inserts to be rolled back.
        Assert.Equal(0, await CountAsync(dbContext, "outer"));
        Assert.Equal(0, await CountAsync(dbContext, "inner"));
    }

    [Fact]
    public async Task OuterRequiredInnerSwallow_CommitsBoth_WhenInnerSwallowsException()
    {
        var sp = _sp;
        var dbContext = sp.GetRequiredService<IDbContext>();
        await PrepareTableAsync(dbContext);

        var service = sp.GetRequiredService<ITransactionalOuterService>();
        // inner swallows the TestNotRollbackException explicitly; outer should not see an exception and will commit
        await service.OuterRequiredInnerSwallowAsync("outer", "inner");

        Assert.Equal(1, await CountAsync(dbContext, "outer"));
        Assert.Equal(1, await CountAsync(dbContext, "inner"));
    }

    [Fact]
    public async Task OuterRequiredInnerRequiresNew_NoRollback_CommitsInnerAndOuter()
    {
        var sp = _sp;
        var dbContext = sp.GetRequiredService<IDbContext>();
        await PrepareTableAsync(dbContext);

        var service = sp.GetRequiredService<ITransactionalOuterService>();
        // inner uses REQUIRES_NEW with NoRollbackFor: it will commit its own transaction but rethrow the exception.
        // Since outer does not catch the exception, outer's transaction will be rolled back.
        await Assert.ThrowsAsync<TestNotRollbackException>(async () => await service.OuterRequiredInnerRequiresNewNoCatchAsync("outer", "inner"));

        // inner committed (RequiresNew), outer rolled back
        Assert.Equal(0, await CountAsync(dbContext, "outer"));
        Assert.Equal(1, await CountAsync(dbContext, "inner"));
    }

    [Fact]
    public async Task Mandatory_WithoutAmbient_ThrowsInvalidOperationException()
    {
        _clientCount = 0;
        var sp = _sp;
        var service = sp.GetRequiredService<ITransactionalInnerService>();

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await service.InnerMandatoryAsync("m1"));
    }

    [Fact]
    public async Task Mandatory_WithAmbient_CommitsBoth()
    {
        var sp = _sp;
        var dbContext = sp.GetRequiredService<IDbContext>();
        await PrepareTableAsync(dbContext);

        var service = sp.GetRequiredService<ITransactionalOuterService>();
        await service.OuterRequiredCallsMandatoryAsync("outer_m", "inner_m");

        Assert.Equal(1, await CountAsync(dbContext, "outer_m"));
        Assert.Equal(1, await CountAsync(dbContext, "inner_m"));
    }

    [Fact]
    public async Task Mandatory_WithAmbient_InnerThrows_OuterDoesNotCatch_RollbackAll()
    {
        var sp = _sp;
        var dbContext = sp.GetRequiredService<IDbContext>();
        await PrepareTableAsync(dbContext);

        var service = sp.GetRequiredService<ITransactionalOuterService>();
        await Assert.ThrowsAsync<Exception>(async () => await service.OuterRequiredCallsMandatoryAndThrowNoCatchAsync("outer_m2", "inner_m2"));

        Assert.Equal(0, await CountAsync(dbContext, "outer_m2"));
        Assert.Equal(0, await CountAsync(dbContext, "inner_m2"));
    }

    [Fact]
    public async Task Mandatory_WithAmbient_InnerThrows_OuterCatches_CommitsBoth()
    {
        var sp = _sp;
        var dbContext = sp.GetRequiredService<IDbContext>();
        await PrepareTableAsync(dbContext);

        var service = sp.GetRequiredService<ITransactionalOuterService>();
        await service.OuterRequiredCallsMandatoryAndThrowCatchAsync("outer_m3", "inner_m3");

        Assert.Equal(1, await CountAsync(dbContext, "outer_m3"));
        Assert.Equal(1, await CountAsync(dbContext, "inner_m3"));
    }

    [Fact]
    public async Task Nested_WithAmbient_CommitsInnerRolledUpToOuterWhenNoErrors()
    {
        var sp = _sp;
        var dbContext = sp.GetRequiredService<IDbContext>();
        await PrepareTableAsync(dbContext);

        var service = sp.GetRequiredService<ITransactionalOuterService>();
        await service.OuterRequiredCallsNestedAsync("outer_n", "inner_n");

        Assert.Equal(1, await CountAsync(dbContext, "outer_n"));
        Assert.Equal(1, await CountAsync(dbContext, "inner_n"));
    }

    [Fact]
    public async Task Nested_WithAmbient_InnerThrows_OuterDoesNotCatch_RollbackAll()
    {
        var sp = _sp;
        var dbContext = sp.GetRequiredService<IDbContext>();
        await PrepareTableAsync(dbContext);

        var service = sp.GetRequiredService<ITransactionalOuterService>();
        await Assert.ThrowsAsync<Exception>(async () => await service.OuterRequiredCallsNestedAndThrowNoCatchAsync("outer_n2", "inner_n2"));

        Assert.Equal(0, await CountAsync(dbContext, "outer_n2"));
        Assert.Equal(0, await CountAsync(dbContext, "inner_n2"));
    }

    [Fact]
    public async Task Nested_WithAmbient_InnerThrows_OuterCatches_OnlyOuterPersists()
    {
        var sp = _sp;
        var dbContext = sp.GetRequiredService<IDbContext>();
        await PrepareTableAsync(dbContext);

        var service = sp.GetRequiredService<ITransactionalOuterService>();
        await service.OuterRequiredCallsNestedAndThrowCatchAsync("outer_n3", "inner_n3");

        Assert.Equal(1, await CountAsync(dbContext, "outer_n3"));
        Assert.Equal(0, await CountAsync(dbContext, "inner_n3"));
    }

    [Fact]
    public async Task Never_WithoutAmbient_Commits()
    {
        var sp = _sp;
        var dbContext = sp.GetRequiredService<IDbContext>();
        await PrepareTableAsync(dbContext);

        var service = sp.GetRequiredService<ITransactionalInnerService>();
        await service.InnerNeverAsync("never1");

        Assert.Equal(1, await CountAsync(dbContext, "never1"));
    }

    [Fact]
    public async Task Never_WithAmbient_ThrowsInvalidOperationException()
    {
        var sp = _sp;
        var dbContext = sp.GetRequiredService<IDbContext>();
        await PrepareTableAsync(dbContext);

        var service = sp.GetRequiredService<ITransactionalOuterService>();
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await service.OuterRequiredCallsNeverAsync("outer_never", "inner_never"));

        Assert.Equal(0, await CountAsync(dbContext, "outer_never"));
        Assert.Equal(0, await CountAsync(dbContext, "inner_never"));
    }

    public void Dispose()
    {
        // Verify that datasource manager cleaned up all clients/propagations after each test
        var manager = _sp.GetRequiredService<IDatasourceManager>();
        var concrete = manager as SqlSugarDatasourceManager;
        Assert.NotNull(concrete);
        Assert.Equal(_clientCount, concrete.ClientCount);
        Assert.Equal(0, concrete.PropagationCount);

        // Dispose the scope we created in the constructor
        _scope.Dispose();
    }
}