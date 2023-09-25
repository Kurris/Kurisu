namespace Kurisu.EFSharding.Sharding.MergeEngines.Common;

internal class DataSourceSqlExecutorUnit
{
    public List<SqlExecutorGroup<SqlExecutorUnit>> SqlExecutorGroups { get; }

    public DataSourceSqlExecutorUnit(List<SqlExecutorGroup<SqlExecutorUnit>> sqlExecutorGroups)
    {
        SqlExecutorGroups = sqlExecutorGroups;
    }
}