using System;

namespace Kurisu.DataAccess.Functions.Default.Abstractions;

/// <summary>
/// 数据库连接字符串处理器
/// </summary>
public interface IDbConnectStringResolver
{
    /// <summary>
    /// 获取连接字符串
    /// </summary>
    /// <param name="dbType">数据库类型</param>
    /// <returns></returns>
    string GetConnectionString(Type dbType);
}