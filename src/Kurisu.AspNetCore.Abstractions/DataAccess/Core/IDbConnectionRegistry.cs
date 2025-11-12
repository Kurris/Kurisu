namespace Kurisu.AspNetCore.Abstractions.DataAccess.Core;

/// <summary>
/// 数据库连接字符串注册表
/// </summary>
public interface IDbConnectionRegistry
{
    void Register(Dictionary<string, string> connectionStrings);
    void Register(string name, string connectionString);

    string GetConnectionString(string name);

    bool Exists(string name);
}