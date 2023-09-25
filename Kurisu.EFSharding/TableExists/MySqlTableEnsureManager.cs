using Kurisu.EFSharding.Core.VirtualRoutes.TableRoutes.RouteTails.Abstractions;
using Kurisu.EFSharding.Sharding.Abstractions;
using Kurisu.EFSharding.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Kurisu.EFSharding.TableExists;

internal class MySqlTableEnsureManager : ITableEnsureManager
{
    private readonly IRouteTailFactory _routeTailFactory;
    private const string Tables = "Tables";
    private const string TableSchema = "TABLE_SCHEMA";
    private const string TableName = "TABLE_NAME";

    public MySqlTableEnsureManager(IRouteTailFactory routeTailFactory)
    {
        _routeTailFactory = routeTailFactory;
    }

    public async Task<ISet<string>> GetExistTablesAsync(IShardingDbContext shardingDbContext, string datasourceName)
    {
        await using var dbContext = shardingDbContext.GetIndependentWriteDbContext(datasourceName, _routeTailFactory.Create(string.Empty));
        var dbConnection = dbContext.Database.GetDbConnection();
        await dbConnection.OpenAsync();

        var database = dbConnection.Database;
        ISet<string> result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var dataTable = await dbConnection.GetSchemaAsync(Tables);

        for (var i = 0; i < dataTable.Rows.Count; i++)
        {
            var schema = dataTable.Rows[i][TableSchema];
            if (database.Equals(schema.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                var tableName = dataTable.Rows[i][TableName].ToString()!;
                result.Add(tableName);
            }
        }

        return result;
    }
}