using System.Threading.Tasks;

namespace Kurisu.Test.DataAccess.Trans.Mock;

/// <summary>
/// 用于验证 [Datasource] 属性与数据源作用域切换行为的测试服务接口
/// </summary>
public interface IDatasourceScopeService
{
    /// <summary>
    /// [Datasource] 无参（同名数据源）：与调用方共用连接和事务，数据提交随外层事务决定
    /// </summary>
    Task InsertInDatasourceScopeAsync(string name);

    /// <summary>
    /// [Datasource("SecondConnectionString")]：切换到独立连接，自动提交，不受外层事务影响
    /// </summary>
    Task InsertInSecondDatasourceScopeAsync(string name);

    /// <summary>
    /// [Datasource("SecondConnectionString")] + [Transactional]：在独立连接上开启事务并提交
    /// </summary>
    Task InsertInDatasourceScopeWithTransactionAsync(string name);

    /// <summary>
    /// [Datasource("SecondConnectionString")] + [Transactional] 抛出异常触发回滚
    /// </summary>
    Task InsertInDatasourceScopeWithTransactionAndThrowAsync(string name);

    /// <summary>
    /// 外层事务中，内层通过 [Datasource("SecondConnectionString")] 独立提交，
    /// 验证外层回滚不影响内层已提交数据
    /// </summary>
    Task InsertInnerWithDatasourceScopeOuterTransactionRollbackAsync(string outerName, string innerName);
}
