namespace Kurisu.AspNetCore.DataAccess.SqlSugar.Services;

/// <summary>
/// 数据库连接工厂
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>
    /// 获取连接字符串
    /// </summary>
    /// <returns></returns>
    string GetConnectionString();
}