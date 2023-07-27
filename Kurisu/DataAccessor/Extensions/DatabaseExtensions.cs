using System.Data;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Kurisu.DataAccessor.Extensions;

public static class DatabaseExtensions
{
    public static async Task<bool> IsTableExistsAsync(this DatabaseFacade database, string tableName)
    {
        using var conn = database.GetDbConnection();
        {
            //var t = _dbContext.Model.FindEntityType(typeof(Entity.Test).FullName);

            if (conn.State.Equals(ConnectionState.Closed))
                await conn.OpenAsync();

            using var command = conn.CreateCommand();

            command.CommandText = $@"
            select 1 from INFORMATION_SCHEMA.TABLES  
            where TABLE_SCHEMA='{conn.Database}'
            and TABLE_NAME='{tableName}';";

            return await command.ExecuteScalarAsync() != null;
        }
    }

    public static async Task CreateTablesAsync(this DatabaseFacade database)
    {
        RelationalDatabaseCreator databaseCreator = (RelationalDatabaseCreator)database.GetService<IRelationalDatabaseCreator>();
        await databaseCreator.CreateTablesAsync();
    }
}
