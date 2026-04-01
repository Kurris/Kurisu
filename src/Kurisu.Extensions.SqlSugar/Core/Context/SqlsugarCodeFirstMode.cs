using Kurisu.AspNetCore.Abstractions.DataAccess.Core;
using Kurisu.Extensions.SqlSugar.Options;
using SqlSugar;

namespace Kurisu.Extensions.SqlSugar.Core.Context;

public class SqlsugarCodeFirstMode : ICodeFirstMode
{
    private readonly ISqlSugarClient _client;

    public SqlsugarCodeFirstMode(ISqlSugarClient client)
    {
        _client = client;
    }

    public virtual void EnsureDatabaseExists()
    {
        _client.DbMaintenance.CreateDatabase();
    }

    public virtual void EnsureTablesExists(params Type[] tables)
    {
        foreach (var table in tables)
        {
            var tableName = _client.EntityMaintenance.GetTableName(table);
            EnsureTableExists(table, tableName);
        }
    }


    public virtual void EnsureTableExists(Type table)
    {
        EnsureTablesExists(table);
    }

    public virtual void EnsureTableExists(Type table, string tableName)
    {
        _client.CodeFirst.As(table, tableName).InitTables(table);

        if (table.IsAssignableTo(typeof(IIndexConfigurator)))
        {
            var handler = (IIndexConfigurator)Activator.CreateInstance(table)!;
            var indexModels = handler.GetIndexConfigs();
            foreach (var indexModel in indexModels)
            {
                if (!_client.DbMaintenance.IsAnyIndex(indexModel.IndexName))
                {
                    _client.DbMaintenance.CreateIndex(tableName, indexModel.ColumnNames, indexModel.IndexName, indexModel.IsUnique);
                }
            }
        }
    }

}
