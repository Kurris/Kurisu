using Kurisu.AspNetCore.Abstractions.DependencyInjection;

namespace Kurisu.AspNetCore.Abstractions.DataAccess.Core.Context;

/// <summary>
/// 数据库上下文
/// </summary>
[SkipScan]
public interface IDbContext : IFilterOperator, IReadDbContext, IWriteDbContext
{
    /// <summary>
    /// 代码优先模式
    /// </summary>
    public ICodeFirstMode CodeFirst { get; }

    /// <summary>
    /// 服务提供器
    /// </summary>
    public IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// 当前数据源管理器
    /// </summary>
    public IDatasourceManager DatasourceManager { get; }

    /// <summary>
    /// 创建数据源作用域
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    IDisposable CreateDatasourceScope(string name);

    /// <summary>
    /// 创建数据源作用域
    /// </summary>
    /// <returns></returns>
    IDisposable CreateDatasourceScope();
}