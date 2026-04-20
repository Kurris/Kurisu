namespace Kurisu.AspNetCore.Abstractions.DataAccess.Core;

/// <summary>
/// 数据库连接字符串管理器
/// </summary>
public interface IDbConnectionStringManager
{
    /// <summary>
    /// 获取当前连接名称
    /// </summary>
    /// <returns></returns>
    public string Current { get; }

    /// <summary>
    /// 创建指定连接作用域
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    IDisposable CreateScope(string name);

    /// <summary>
    /// 创建指定连接作用域
    /// </summary>
    /// <param name="name"></param>
    /// <param name="onDispose"></param>
    /// <returns></returns>
    IDisposable CreateScope(string name, Action onDispose);

    /// <summary>
    /// 获取指定连接字符串
    /// </summary>
    /// <returns></returns>
    string GetConnectionString(string name);

    /// <summary>
    /// 获取当前连接
    /// </summary>
    /// <returns></returns>
    string GetCurrentConnectionString();
}