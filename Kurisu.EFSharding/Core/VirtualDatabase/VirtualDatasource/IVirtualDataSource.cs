using System.Data.Common;
using Kurisu.EFSharding.Core.VirtualDatabase.VirtualDatasource.Abstractions;
using Kurisu.EFSharding.Exceptions;
using Kurisu.EFSharding.Sharding.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Kurisu.EFSharding.Core.VirtualDatabase.VirtualDatasource;

public interface IVirtualDatasource
{
    /// <summary>
    /// 数据源配置
    /// </summary>
    IVirtualDatasourceConfigurationParams ConfigurationParams { get; }

    /// <summary>
    /// 链接字符串管理
    /// </summary>
    IConnectionStringManager ConnectionStringManager { get; }

    /// <summary>
    /// 是否启用了读写分离
    /// </summary>
    bool UseReadWriteSeparation { get; }

    /// <summary>
    /// 默认的数据源名称
    /// </summary>
    string DefaultDatasourceName { get; }

    /// <summary>
    /// 默认连接字符串
    /// </summary>
    string DefaultConnectionString { get; }


    /// <summary>
    /// 获取默认的数据源信息
    /// </summary>
    /// <returns></returns>
    DatasourceUnit GetDefaultDatasource();

    /// <summary>
    /// 获取数据源
    /// </summary>
    /// <param name="datasourceName"></param>
    /// <exception cref="ShardingCoreNotFoundException">
    ///     thrown if data source name is not in virtual data source
    ///     the length of the buffer
    /// </exception>
    /// <returns></returns>
    DatasourceUnit GetPhysicDatasource(string datasourceName);

    /// <summary>
    /// 获取所有的数据源名称
    /// </summary>
    /// <returns></returns>
    List<string> GetAllDatasourceNames();

    /// <summary>
    /// 获取连接字符串
    /// </summary>
    /// <param name="datasourceName"></param>
    /// <returns></returns>
    /// <exception cref="ShardingCoreNotFoundException"></exception>
    string GetConnectionString(string datasourceName);

    /// <summary>
    /// 添加数据源
    /// </summary>
    /// <param name="physicDatasource"></param>
    /// <returns></returns>
    /// <exception cref="ShardingCoreInvalidOperationException">重复添加默认数据源</exception>
    bool AddPhysicDatasource(DatasourceUnit physicDatasource);

    /// <summary>
    /// 是否默认数据源
    /// </summary>
    /// <param name="datasourceName"></param>
    /// <returns></returns>
    bool IsDefault(string datasourceName);

    /// <summary>
    /// 检查是否配置默认数据源和默认链接字符串
    /// </summary>
    /// <exception cref="ShardingCoreInvalidOperationException"></exception>
    void CheckVirtualDatasource();

    /// <summary>
    /// 如何根据connectionString 配置 DbContextOptionsBuilder
    /// </summary>
    /// <param name="connectionString"></param>
    /// <param name="dbContextOptionsBuilder"></param>
    /// <returns></returns>
    DbContextOptionsBuilder UseDbContextOptionsBuilder(string connectionString, DbContextOptionsBuilder dbContextOptionsBuilder);

    /// <summary>
    /// 如何根据dbConnection 配置DbContextOptionsBuilder
    /// </summary>
    /// <param name="dbConnection"></param>
    /// <param name="dbContextOptionsBuilder"></param>
    /// <returns></returns>
    DbContextOptionsBuilder UseDbContextOptionsBuilder(DbConnection dbConnection, DbContextOptionsBuilder dbContextOptionsBuilder);

    IDictionary<string, string> GetDatasource();
}