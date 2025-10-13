using Kurisu.AspNetCore.Abstractions.DataAccess;
using Kurisu.AspNetCore.Extensions;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;

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
}