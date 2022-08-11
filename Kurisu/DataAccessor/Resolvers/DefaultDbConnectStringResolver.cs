using System;
using Kurisu.DataAccessor.Resolvers.Abstractions;
using Microsoft.Extensions.Options;

namespace Kurisu.DataAccessor.Resolvers
{
    /// <summary>
    /// 默认数据库连接字符串处理器
    /// </summary>
    public class DefaultDbConnectStringResolver : IDbConnectStringResolver
    {
        private readonly DbSetting _dbSetting;

        public DefaultDbConnectStringResolver(IOptions<DbSetting> options)
        {
            _dbSetting = options?.Value;
        }


        public virtual string GetConnectionString(Type dbType) => _dbSetting.DefaultConnectionString;
    }
}