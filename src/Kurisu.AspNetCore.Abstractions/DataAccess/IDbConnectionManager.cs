namespace Kurisu.AspNetCore.Abstractions.DataAccess;

/// <summary>
/// 数据库连接字符串管理器
/// </summary>
public interface IDbConnectionManager
{
    string GetCurrent();

    void Switch(string name);
    void Switch();

    void Register(string name, string connectionString);

    /// <summary>
    /// 获取连接字符串
    /// </summary>
    /// <returns></returns>
    string GetConnectionString(string name);

    string GetCurrentConnectionString();
}