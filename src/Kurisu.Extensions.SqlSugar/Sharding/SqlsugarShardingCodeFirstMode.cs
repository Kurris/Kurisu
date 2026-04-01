using Kurisu.Extensions.SqlSugar.Core.Context;
using SqlSugar;

namespace Kurisu.Extensions.SqlSugar.Sharding;

internal class SqlsugarShardingCodeFirstMode : SqlsugarCodeFirstMode
{
    private readonly ISqlSugarClient _client;

    public SqlsugarShardingCodeFirstMode(ISqlSugarClient client) : base(client)
    {
        _client = client;
    }

    public override void EnsureTablesExists(params Type[] tables)
    {
        var suffixs = _client.Queryable<ShardingRouteTable>().Select(x => x.TableSuffix).ToList();
        foreach (var table in tables)
        {
            if (table.IsAssignableTo(typeof(IShardingRoute)))
            {
                var originalTable = _client.EntityMaintenance.GetTableName(table);
                foreach (var suffix in suffixs)
                {
                    var tableName = $"{originalTable}_{suffix}";
                    EnsureTableExists(table, tableName);
                }
            }
            else
            {
                base.EnsureTablesExists(table);
            }
        }
    }
}
