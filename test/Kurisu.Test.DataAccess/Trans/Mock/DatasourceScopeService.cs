using Kurisu.AspNetCore.Abstractions.DataAccess;
using Kurisu.AspNetCore.Abstractions.DataAccess.Aop;
using Kurisu.AspNetCore.Abstractions.DataAccess.Core.Context;

namespace Kurisu.Test.DataAccess.Trans.Mock;

/// <summary>
/// 数据源作用域测试服务实现
/// </summary>
public class DatasourceScopeService : IDatasourceScopeService
{
    private readonly IDbContext _dbContext;

    public DatasourceScopeService(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// [Datasource] 无参（同名数据源）：同名即为当前数据源，返回无操作，
    /// 连接与事务与调用方共用，数据提交随外层事务决定
    /// </summary>
    [Datasource]
    public async Task InsertInDatasourceScopeAsync(string name)
    {
        await _dbContext.InsertAsync(new TxTest { Name = name });
    }

    /// <summary>
    /// [Datasource("SecondConnectionString")] 切换到不同数据源：
    /// 使用独立连接，自动提交，不受外层事务影响
    /// </summary>
    [Datasource("SecondConnectionString")]
    public async Task InsertInSecondDatasourceScopeAsync(string name)
    {
        await _dbContext.InsertAsync(new TxTest { Name = name });
    }

    /// <summary>
    /// [Datasource("SecondConnectionString")] + [Transactional]：在独立连接上开启事务并提交，
    /// 事务作用于切换后的管理器，与外层（默认数据源）管理器完全隔离
    /// </summary>
    [Datasource("SecondConnectionString")]
    [Transactional]
    public async Task InsertInDatasourceScopeWithTransactionAsync(string name)
    {
        await _dbContext.InsertAsync(new TxTest { Name = name });
    }

    /// <summary>
    /// [Datasource("SecondConnectionString")] + [Transactional] 抛出异常：
    /// 回滚只发生在切换后的管理器上，外层（默认数据源）管理器不受影响
    /// </summary>
    [Datasource("SecondConnectionString")]
    [Transactional]
    public async Task InsertInDatasourceScopeWithTransactionAndThrowAsync(string name)
    {
        await _dbContext.InsertAsync(new TxTest { Name = name });
        throw new Exception("datasource scope inner rollback");
    }

    /// <summary>
    /// 外层持有事务（Required），内层通过 [Datasource("SecondConnectionString")] 使用独立连接提交后，
    /// 外层事务回滚，验证内层数据已提交且不被外层回滚波及
    /// </summary>
    [Transactional]
    public async Task InsertInnerWithDatasourceScopeOuterTransactionRollbackAsync(string outerName, string innerName)
    {
        // 外层事务：通过当前（默认数据源）管理器插入
        await _dbContext.InsertAsync(new TxTest { Name = outerName });

        // 内层独立连接（切换到 SecondConnectionString）：插入后自动提交
        await InsertInSecondDatasourceScopeAsync(innerName);

        // 外层抛出异常触发回滚
        throw new Exception("outer rollback, inner should survive");
    }
}
