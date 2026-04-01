namespace Kurisu.AspNetCore.Abstractions.DataAccess.Core;

/// <summary>
/// 数据库连接字符串注册表
/// </summary>
public interface IDbConnectionRegistry
{
    /// <summary>
    /// 注册
    /// </summary>
    /// <param name="name"></param>
    /// <param name="connectionString"></param>
    void Register(string name, string connectionString);

    /// <summary>
    /// 注册
    /// </summary>
    /// <param name="connectionStrings"></param>
    void Register(Dictionary<string, string> connectionStrings);

    /// <summary>
    /// 根据名称获取连接字符串
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    string GetConnectionString(string name);

    /// <summary>
    /// 是否存在指定名称的连接字符串 
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    bool Exists(string name);
}