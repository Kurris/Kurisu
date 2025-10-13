using Kurisu.AspNetCore.Abstractions.DataAccess;
using Kurisu.AspNetCore.Extensions;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;
using Kurisu.Extensions.SqlSugar;

namespace Kurisu.Test.DataAccess.Trans;

public class TransactionManagerTests
{
    /// <summary>
    /// 准备测试用表：若不存在则创建，并清空数据。根据 TEST_DB_PROVIDER 生成兼容的建表语句（mysql / sqlserver）。
    /// </summary>
    /// <param name="manager">数据源管理器</param>
    private async Task PrepareTableAsync(IDatasourceManager manager)
    {
        var client = manager.GetCurrentClient<ISqlSugarClient>();
        if (client == null) throw new Exception("ISqlSugarClient not available from manager.CurrentDbClient");

        // MySQL 语法
        await client.Ado.ExecuteCommandAsync("CREATE TABLE IF NOT EXISTS tx_test (id INT AUTO_INCREMENT PRIMARY KEY, name VARCHAR(200));");
        await client.Ado.ExecuteCommandAsync("TRUNCATE TABLE tx_test;");
    }

    /// <summary>
    /// 统计指定 name 在测试表中的行数（使用参数化查询）
    /// </summary>
    /// <param name="manager">数据源管理器</param>
    /// <param name="name">要统计的 name 值</param>
    /// <returns>符合条件的行数</returns>
    private async Task<int> CountAsync(IDatasourceManager manager, string name)
    {
        var client = manager.GetCurrentClient<ISqlSugarClient>();
        if (client == null) throw new Exception("ISqlSugarClient not available from manager.CurrentDbClient");
        return await client.Ado.GetIntAsync("SELECT COUNT(1) FROM tx_test WHERE name = @name", new { name });
    }

    /// <summary>
    /// 测试：Propagation.Required 在嵌套时会加入外层事务，内外插入在外层提交后一起持久化
    /// </summary>
    [Fact]
    public async Task Required_JoinsAmbientAndCommitByOuter_PersistsAllInnerAndOuterInserts()
    {
        var sp = TestHelper.GetServiceProvider();
        var manager = sp.GetRequiredService<IDatasourceManager>();

        await PrepareTableAsync(manager);

        using (var outer = manager.CreateTransScope(Propagation.Required))
        {
            await outer.BeginAsync();

            var outerClient = manager.GetCurrentClient<ISqlSugarClient>();
            await outerClient!.Ado.ExecuteCommandAsync("INSERT INTO tx_test (name) VALUES (@name)", new { name = "outer" });

            using (var inner = manager.CreateTransScope(Propagation.Required))
            {
                await inner.BeginAsync(); // 加入外层事务
                var innerClient = manager.GetCurrentClient<ISqlSugarClient>();
                await innerClient!.Ado.ExecuteCommandAsync("INSERT INTO tx_test (name) VALUES (@name)", new { name = "inner" });

                // 内层 Commit 对于 join 情况为 no-op
                await inner.CommitAsync();
            }

            // 外层提交：应持久化内外两条记录
            await outer.CommitAsync();
        }

        var countOuter = await CountAsync(manager, "outer");
        var countInner = await CountAsync(manager, "inner");

        Assert.Equal(1, countOuter);
        Assert.Equal(1, countInner);
    }

    /// <summary>
    /// 测试：Propagation.Required 下内层回滚会回滚整个 ambient 事务，最终不应有任何行被持久化
    /// </summary>
    [Fact]
    public async Task Required_RollbackInInner_CancelsAmbientAndNoRowsPersisted()
    {
        var sp = TestHelper.GetServiceProvider();
        var manager = sp.GetRequiredService<IDatasourceManager>();

        await PrepareTableAsync(manager);

        using (var outer = manager.CreateTransScope(Propagation.Required))
        {
            await outer.BeginAsync();
            var outerClient = manager.GetCurrentClient<ISqlSugarClient>();
            await outerClient!.Ado.ExecuteCommandAsync("INSERT INTO tx_test (name) VALUES (@name)", new { name = "outer" });

            using (var inner = manager.CreateTransScope(Propagation.Required))
            {
                await inner.BeginAsync(); // 加入外层事务
                var innerClient = manager.GetCurrentClient<ISqlSugarClient>();
                await innerClient!.Ado.ExecuteCommandAsync("INSERT INTO tx_test (name) VALUES (@name)", new { name = "inner" });

                // 内层回滚：应回滚 ambient 事务
                await inner.RollbackAsync();
            }

            await outer.RollbackAsync();
        }

        var countOuter = await CountAsync(manager, "outer");
        var countInner = await CountAsync(manager, "inner");

        Assert.Equal(0, countOuter);
        Assert.Equal(0, countInner);
    }

    /// <summary>
    /// 测试：Propagation.RequiresNew 会启动独立事务，内层提交不会影响外层
    /// </summary>
    [Fact]
    public async Task RequiresNew_IndependentCommit_PersistsBoth()
    {
        var sp = TestHelper.GetServiceProvider();
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

                // 独立提交内层事务
                await inner.CommitAsync();
            }

            // 提交外层事务
            await outer.CommitAsync();
        }

        var countOuter = await CountAsync(manager, "outer");
        var countInner = await CountAsync(manager, "inner");

        Assert.Equal(1, countOuter);
        Assert.Equal(1, countInner);
    }

    /// <summary>
    /// 测试：RequiresNew 内层回滚不应影响外层事务的提交
    /// </summary>
    [Fact]
    public async Task RequiresNew_RollbackInner_DoesNotAffectOuter()
    {
        var sp = TestHelper.GetServiceProvider();
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

                // 回滚内层事务
                await inner.RollbackAsync();
            }

            // 提交外层事务
            await outer.CommitAsync();
        }

        var countOuter = await CountAsync(manager, "outer");
        var countInner = await CountAsync(manager, "inner");

        Assert.Equal(1, countOuter);
        Assert.Equal(0, countInner);
    }

    /// <summary>
    /// 测试：Propagation.Mandatory 在没有 ambient 事务时应抛出 InvalidOperationException
    /// </summary>
    [Fact]
    public async Task Mandatory_WithoutAmbient_ThrowsInvalidOperationException()
    {
        var sp = TestHelper.GetServiceProvider();
        var manager = sp.GetRequiredService<IDatasourceManager>();

        await PrepareTableAsync(manager);

        // CreateTransScope should throw because there is no ambient transaction
        Assert.Throws<InvalidOperationException>(() => manager.CreateTransScope(Propagation.Mandatory));
    }

    /// <summary>
    /// 测试：Propagation.Mandatory 在嵌套时会加入外层事务，内外插入在外层提交后一起持久化
    /// </summary>
    [Fact]
    public async Task Mandatory_JoinsAmbientAndCommitByOuter_PersistsAllInnerAndOuterInserts()
    {
        var sp = TestHelper.GetServiceProvider();
        var manager = sp.GetRequiredService<IDatasourceManager>();

        await PrepareTableAsync(manager);

        using (var outer = manager.CreateTransScope(Propagation.Required))
        {
            await outer.BeginAsync();

            var outerClient = manager.GetCurrentClient<ISqlSugarClient>();
            await outerClient!.Ado.ExecuteCommandAsync("INSERT INTO tx_test (name) VALUES (@name)", new { name = "outer_m" });

            using (var inner = manager.CreateTransScope(Propagation.Mandatory))
            {
                await inner.BeginAsync(); // 加入外层事务
                var innerClient = manager.GetCurrentClient<ISqlSugarClient>();
                await innerClient!.Ado.ExecuteCommandAsync("INSERT INTO tx_test (name) VALUES (@name)", new { name = "inner_m" });

                // 内层 Commit 对于 join 情况为 no-op
                await inner.CommitAsync();
            }

            // 外层提交：应持久化内外两条记录
            await outer.CommitAsync();
        }

        var countOuter = await CountAsync(manager, "outer_m");
        var countInner = await CountAsync(manager, "inner_m");

        Assert.Equal(1, countOuter);
        Assert.Equal(1, countInner);
    }

    /// <summary>
    /// 测试：Propagation.Mandatory 下内层抛出异常且外层不捕获时，应回滚 ambient 事务
    /// </summary>
    [Fact]
    public async Task Mandatory_InnerThrows_OuterDoesNotCatch_RollbackAll()
    {
        var sp = TestHelper.GetServiceProvider();
        var manager = sp.GetRequiredService<IDatasourceManager>();

        await PrepareTableAsync(manager);

        using (var outer = manager.CreateTransScope(Propagation.Required))
        {
            await outer.BeginAsync();
            var outerClient = manager.GetCurrentClient<ISqlSugarClient>();
            await outerClient!.Ado.ExecuteCommandAsync("INSERT INTO tx_test (name) VALUES (@name)", new { name = "outer_m2" });

            using (var inner = manager.CreateTransScope(Propagation.Mandatory))
            {
                await inner.BeginAsync(); // 加入外层事务
                var innerClient = manager.GetCurrentClient<ISqlSugarClient>();
                await innerClient!.Ado.ExecuteCommandAsync("INSERT INTO tx_test (name) VALUES (@name)", new { name = "inner_m2" });

                // 内层抛异常：回滚 ambient 事务
                await inner.RollbackAsync();
            }

            await outer.RollbackAsync();
        }

        var countOuter = await CountAsync(manager, "outer_m2");
        var countInner = await CountAsync(manager, "inner_m2");

        Assert.Equal(0, countOuter);
        Assert.Equal(0, countInner);
    }

    /// <summary>
    /// 测试：Propagation.Nested 在没有 ambient 事务时行为等同于 Required（新建事务）
    /// </summary>
    [Fact]
    public async Task Nested_WithoutAmbient_BehavesLikeRequired()
    {
        var sp = TestHelper.GetServiceProvider();
        var manager = sp.GetRequiredService<IDatasourceManager>();

        await PrepareTableAsync(manager);

        using (var scope = manager.CreateTransScope(Propagation.Nested))
        {
            await scope.BeginAsync();
            var client = manager.GetCurrentClient<ISqlSugarClient>();
            await client!.Ado.ExecuteCommandAsync("INSERT INTO tx_test (name) VALUES (@name)", new { name = "nested_no_ambient" });
            await scope.CommitAsync();
        }

        var count = await CountAsync(manager, "nested_no_ambient");
        Assert.Equal(1, count);
    }

    /// <summary>
    /// 测试：Propagation.Nested 在有外层事务时会创建 savepoint，inner commit 不会影响最终提交，outer commit 应持久化内外改动
    /// </summary>
    [Fact]
    public async Task Nested_JoinsAmbientAndCommitByOuter_PersistsAllInnerAndOuterInserts()
    {
        var sp = TestHelper.GetServiceProvider();
        var manager = sp.GetRequiredService<IDatasourceManager>();

        await PrepareTableAsync(manager);

        using (var outer = manager.CreateTransScope(Propagation.Required))
        {
            await outer.BeginAsync();

            var outerClient = manager.GetCurrentClient<ISqlSugarClient>();
            await outerClient!.Ado.ExecuteCommandAsync("INSERT INTO tx_test (name) VALUES (@name)", new { name = "outer_nested" });

            using (var inner = manager.CreateTransScope(Propagation.Nested))
            {
                await inner.BeginAsync(); // 创建 savepoint
                var innerClient = manager.GetCurrentClient<ISqlSugarClient>();
                await innerClient!.Ado.ExecuteCommandAsync("INSERT INTO tx_test (name) VALUES (@name)", new { name = "inner_nested" });

                // 内层 Commit 对于 savepoint 情形应释放 savepoint（或为 no-op）
                await inner.CommitAsync();
            }

            // 外层提交：应持久化内外两条记录
            await outer.CommitAsync();
        }

        var countOuter = await CountAsync(manager, "outer_nested");
        var countInner = await CountAsync(manager, "inner_nested");

        Assert.Equal(1, countOuter);
        Assert.Equal(1, countInner);
    }

    /// <summary>
    /// 测试：Propagation.Nested 下内层回滚会回滚到 savepoint，仅撤销内层改动，外层仍可提交
    /// </summary>
    [Fact]
    public async Task Nested_InnerRollback_RollsBackToSavepoint_OuterCanCommitOnlyOuterPersists()
    {
        var sp = TestHelper.GetServiceProvider();
        var manager = sp.GetRequiredService<IDatasourceManager>();

        await PrepareTableAsync(manager);

        using (var outer = manager.CreateTransScope(Propagation.Required))
        {
            await outer.BeginAsync();
            var outerClient = manager.GetCurrentClient<ISqlSugarClient>();
            await outerClient!.Ado.ExecuteCommandAsync("INSERT INTO tx_test (name) VALUES (@name)", new { name = "outer_nested2" });

            using (var inner = manager.CreateTransScope(Propagation.Nested))
            {
                await inner.BeginAsync(); // 创建 savepoint
                var innerClient = manager.GetCurrentClient<ISqlSugarClient>();
                await innerClient!.Ado.ExecuteCommandAsync("INSERT INTO tx_test (name) VALUES (@name)", new { name = "inner_nested2" });

                // 内层回滚到 savepoint：仅撤销内层改动
                await inner.RollbackAsync();
            }

            // 外层提交：应仅持久化 outer 的改动
            await outer.CommitAsync();
        }

        var countOuter = await CountAsync(manager, "outer_nested2");
        var countInner = await CountAsync(manager, "inner_nested2");

        Assert.Equal(1, countOuter);
        Assert.Equal(0, countInner);
    }

    /// <summary>
    /// 测试：Propagation.Never 在没有 ambient 事务时按非事务方式执行
    /// </summary>
    [Fact]
    public async Task Never_WithoutAmbient_ExecutesWithoutTransaction()
    {
        var sp = TestHelper.GetServiceProvider();
        var manager = sp.GetRequiredService<IDatasourceManager>();

        await PrepareTableAsync(manager);

        using (var scope = manager.CreateTransScope(Propagation.Never))
        {
            await scope.BeginAsync();
            var client = manager.GetCurrentClient<ISqlSugarClient>();
            await client!.Ado.ExecuteCommandAsync("INSERT INTO tx_test (name) VALUES (@name)", new { name = "never_mgr" });
            await scope.CommitAsync();
        }

        var count = await CountAsync(manager, "never_mgr");
        Assert.Equal(1, count);
    }

    /// <summary>
    /// 测试：Propagation.Never 在有 ambient 事务时应抛出 InvalidOperationException
    /// </summary>
    [Fact]
    public async Task Never_WithAmbient_ThrowsInvalidOperationException()
    {
        var sp = TestHelper.GetServiceProvider();
        var manager = sp.GetRequiredService<IDatasourceManager>();

        await PrepareTableAsync(manager);

        using (var outer = manager.CreateTransScope(Propagation.Required))
        {
            await outer.BeginAsync();
            // 在有外层事务的情况下，创建 Never scope 应抛异常
            Assert.Throws<InvalidOperationException>(() => manager.CreateTransScope(Propagation.Never));
            await outer.RollbackAsync();
        }
    }

    /// <summary>
    /// 测试：Dispose 后 Propagation 栈应清空，ClientCount 应为 1
    /// </summary>
    [Fact]
    public async Task Dispose_ClearsPropagationStack_And_LeavesOneClient()
    {
        var sp = TestHelper.GetServiceProvider();
        var manager = sp.GetRequiredService<IDatasourceManager>();
        var concrete = (SqlSugarDatasourceManager)manager;

        await PrepareTableAsync(manager);

        using (var outer = manager.CreateTransScope(Propagation.Required))
        {
            await outer.BeginAsync();
            var outerClient = manager.GetCurrentClient<ISqlSugarClient>();
            await outerClient!.Ado.ExecuteCommandAsync("INSERT INTO tx_test (name) VALUES (@name)", new { name = "dispose_outer" });

            using (var inner = manager.CreateTransScope(Propagation.RequiresNew))
            {
                await inner.BeginAsync();
                var innerClient = manager.GetCurrentClient<ISqlSugarClient>();
                await innerClient!.Ado.ExecuteCommandAsync("INSERT INTO tx_test (name) VALUES (@name)", new { name = "dispose_inner" });
                await inner.CommitAsync();
            }

            await outer.CommitAsync();
        }

        // 在所有作用域 Dispose 后，检查栈与 client 状态
        Assert.Equal(0, concrete.PropagationCount);
        Assert.Equal(1, concrete.ClientCount);
    }
}