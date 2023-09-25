using System.Data.Common;
using Kurisu.EFSharding.Sharding.ReadWriteConfigurations;
using Microsoft.EntityFrameworkCore;

namespace Kurisu.EFSharding.Core.VirtualDatabase.VirtualDatasource.Abstractions;

internal abstract class AbstractVirtualDatasourceConfigurationParams : IVirtualDatasourceConfigurationParams
{
    public virtual int MaxQueryConnectionsLimit { get; } = Environment.ProcessorCount;
    public virtual ConnectionModeEnum ConnectionMode { get; } = ConnectionModeEnum.Auto;
    public abstract List<DatasourceUnit> Datasources { get; }
    public virtual IDictionary<string, ReadNode[]> ReadWriteNodeSeparationConfigs { get; }
    public virtual ReadStrategyEnum? ReadStrategy { get; }
    public virtual ReadWriteDefaultEnableBehavior? ReadWriteDefaultEnableBehavior { get; }
    public virtual int? ReadWriteDefaultPriority { get; }
    public virtual ReadConnStringGetStrategyEnum? ReadConnStringGetStrategy { get; }

    public abstract DbContextOptionsBuilder UseDbContextOptionsBuilder(string connectionString,
        DbContextOptionsBuilder dbContextOptionsBuilder);

    public abstract DbContextOptionsBuilder UseDbContextOptionsBuilder(DbConnection dbConnection,
        DbContextOptionsBuilder dbContextOptionsBuilder);

    public abstract void UseShellDbContextOptionBuilder(DbContextOptionsBuilder dbContextOptionsBuilder);

    public abstract void UseExecutorDbContextOptionBuilder(DbContextOptionsBuilder dbContextOptionsBuilder);

    public virtual bool UseReadWriteSeparation()
    {
        return ReadWriteNodeSeparationConfigs != null;
    }
}