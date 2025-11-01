namespace Kurisu.AspNetCore.Abstractions.DataAccess;

/// <summary>
/// 数据库连接字符串管理器
/// </summary>
public interface IDbConnectionManager
{
    void Register(string name, string connectionString);

    string GetCurrent();

    void SwitchConnectionString(string name, Action todo);
    Task SwitchConnectionStringAsync(string name, Func<Task> todo);

    /// <summary>
    /// 获取连接字符串
    /// </summary>
    /// <returns></returns>
    string GetConnectionString(string name);

    string GetCurrentConnectionString();
}