using System;
using Kurisu.DataAccessor.Functions.Default.Abstractions;
using Microsoft.Extensions.Options;

namespace Kurisu.DataAccessor.Functions.Default.Resolvers;

/// <summary>
/// 默认数据库连接字符串处理器
/// </summary>
public class DefaultDbConnectStringResolver : IDbConnectStringResolver
{
    private readonly DbSetting _dbSetting;

    public DefaultDbConnectStringResolver(IOptions<DbSetting> options)
    {
        _dbSetting = options.Value;
    }

    /// <summary>
    /// 获取数据库连接字符串
    /// </summary>
    /// <param name="dbType"></param>
    /// <returns></returns>
    public virtual string GetConnectionString(Type dbType) => _dbSetting.DefaultConnectionString;
}