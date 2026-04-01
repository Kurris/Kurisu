namespace Kurisu.AspNetCore.Abstractions.DataAccess.Core;

/// <summary>
/// 代码优先模式
/// </summary>
public interface ICodeFirstMode
{
    /// <summary>
    /// 确保数据库存在
    /// </summary>
    void EnsureDatabaseExists();

    /// <summary>
    /// 确保数据表存在
    /// </summary>
    /// <param name="tables"></param>
    void EnsureTablesExists(params Type[] tables);

    /// <summary>
    /// 确保数据表存在 
    /// </summary>
    /// <param name="table"></param>
    /// <param name="tableName"></param>
    void EnsureTableExists(Type table, string tableName);

    /// <summary>
    /// 确保数据表存在 
    /// </summary>
    /// <param name="table"></param>
    void EnsureTableExists(Type table);
}
