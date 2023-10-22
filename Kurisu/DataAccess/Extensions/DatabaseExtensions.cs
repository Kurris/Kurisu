//using System.Data;
//using System.Threading.Tasks;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.EntityFrameworkCore.Infrastructure;
//using Microsoft.EntityFrameworkCore.Storage;

//namespace Kurisu.DataAccess.Extensions;

///// <summary>
///// 数据库扩展
///// </summary>
//public static class DatabaseExtensions
//{
//    /// <summary>
//    /// 检查表是否存在
//    /// </summary>
//    /// <param name="database"></param>
//    /// <param name="tableName"></param>
//    /// <returns></returns>
//    public static async Task<bool> IsTableExistsAsync(this DatabaseFacade database, string tableName)
//    {
//        await database.CreateAsync();
//        var conn = database.GetDbConnection();
//        //var t = _dbContext.Model.FindEntityType(typeof(Entity.Test).FullName);

//        if (conn.State.Equals(ConnectionState.Closed))
//            await conn.OpenAsync();

//        await using var command = conn.CreateCommand();
//        command.CommandText = $@"
//            select 1 from INFORMATION_SCHEMA.TABLES  
//            where TABLE_SCHEMA='{conn.Database}'
//            and TABLE_NAME='{tableName}';";

//        return await command.ExecuteScalarAsync() != null;
//    }

//    /// <summary>
//    /// 创建表
//    /// </summary>
//    /// <param name="database"></param>
//    public static async Task CreateTablesAsync(this DatabaseFacade database)
//    {
//        RelationalDatabaseCreator databaseCreator = (RelationalDatabaseCreator)database.GetService<IRelationalDatabaseCreator>();
//        await databaseCreator.CreateTablesAsync();
//    }

//    public static async Task CreateAsync(this DatabaseFacade database)
//    {
//        try
//        {
//            RelationalDatabaseCreator databaseCreator = (RelationalDatabaseCreator)database.GetService<IRelationalDatabaseCreator>();
//            await databaseCreator.CreateAsync();
//        }
//        catch (System.Exception)
//        {
//        }
//    }
//}