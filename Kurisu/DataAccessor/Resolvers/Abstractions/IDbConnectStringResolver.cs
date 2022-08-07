using System;

namespace Kurisu.DataAccessor.Abstractions.Setting
{
    /// <summary>
    /// 数据库连接字符串处理器
    /// </summary>
    public interface IDbConnectStringResolver
    {
        /// <summary>
        /// 获取连接字符串
        /// </summary>
        /// <param name="dbType"></param>
        /// <returns></returns>
        string GetConnectionString(Type dbType);
    }
}