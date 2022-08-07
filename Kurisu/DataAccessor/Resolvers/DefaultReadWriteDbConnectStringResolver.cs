using System;
using System.Linq;
using Kurisu.DataAccessor.ReadWriteSplit.Abstractions;
using Microsoft.Extensions.Options;

namespace Kurisu.DataAccessor.Resolvers
{
    /// <summary>
    /// 默认数据库读写分离连接字符串处理器
    /// </summary>
    public class DefaultReadWriteDbConnectStringResolver : DefaultDbConnectStringResolver
    {
        private readonly DbSetting _dbSetting;

        public DefaultReadWriteDbConnectStringResolver(IOptions<DbSetting> options) : base(options)
        {
            _dbSetting = options?.Value;
        }

        public override string GetConnectionString(Type dbType)
        {
            var connectionString = string.Empty;

            if (dbType == typeof(IAppMasterDb))
                connectionString = _dbSetting.DefaultConnectionString;
            else
            {
                if (_dbSetting.ReadConnectionStrings?.Any() == true)
                {
                    var index = new Random().Next(0, _dbSetting.ReadConnectionStrings.Count() - 1);
                    connectionString = _dbSetting.ReadConnectionStrings.ElementAt(index);
                }

                //如果读库连接不存在，则使用默认连接
                if (string.IsNullOrEmpty(connectionString))
                    connectionString = _dbSetting.DefaultConnectionString;
            }

            return connectionString;
        }
    }
}