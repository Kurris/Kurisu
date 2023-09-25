using System.Data.Common;
using Kurisu.EFSharding.Sharding.ReadWriteConfigurations;
using Microsoft.EntityFrameworkCore;

namespace Kurisu.EFSharding.Core.VirtualDatabase.VirtualDatasource.Abstractions;

public interface IVirtualDatasourceConfigurationParams
{
    /// <summary>
    /// 不能小于等于0 should greater than or equal  zero
    /// </summary>
    int MaxQueryConnectionsLimit { get; }

    /// <summary>
    /// 不能为空null,should not null
    /// </summary>
    public List<DatasourceUnit> Datasources { get; }

    /// <summary>
    /// null表示不启用读写分离,if null mean not enable read write
    /// </summary>
    IDictionary<string, ReadNode[]> ReadWriteNodeSeparationConfigs { get; }

    ReadStrategyEnum? ReadStrategy { get; }
    ReadWriteDefaultEnableBehavior? ReadWriteDefaultEnableBehavior { get; }
    int? ReadWriteDefaultPriority { get; }

    /// <summary>
    /// 读写分离链接字符串获取
    /// </summary>
    ReadConnStringGetStrategyEnum? ReadConnStringGetStrategy { get; }

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

    /// <summary>
    /// 外部db context
    /// </summary>
    /// <param name="dbContextOptionsBuilder"></param>
    void UseShellDbContextOptionBuilder(DbContextOptionsBuilder dbContextOptionsBuilder);

    /// <summary>
    /// 真实DbContextOptionBuilder的配置
    /// </summary>
    /// <param name="dbContextOptionsBuilder"></param>
    void UseExecutorDbContextOptionBuilder(DbContextOptionsBuilder dbContextOptionsBuilder);

    /// <summary>
    /// 使用读写分离
    /// </summary>
    /// <returns></returns>
    bool UseReadWriteSeparation();
}