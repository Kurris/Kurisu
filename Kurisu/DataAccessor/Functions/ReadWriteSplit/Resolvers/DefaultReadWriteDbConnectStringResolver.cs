using System;
using System.Linq;
using Kurisu.DataAccessor.Functions.Default.DbContexts;
using Kurisu.DataAccessor.Functions.Default.Resolvers;
using Kurisu.DataAccessor.Functions.ReadWriteSplit.Abstractions;
using Microsoft.Extensions.Options;

namespace Kurisu.DataAccessor.Functions.ReadWriteSplit.Resolvers
{
    /// <summary>
    /// 默认数据库读写分离连接字符串处理器
    /// </summary>
    public class DefaultReadWriteDbConnectStringResolver : DefaultDbConnectStringResolver
    {
        private readonly DbSetting _dbSetting;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="options"></param>
        public DefaultReadWriteDbConnectStringResolver(IOptions<DbSetting> options) : base(options)
        {
            _dbSetting = options.Value;
        }

        /// <summary>
        /// 获取连接字符串
        /// </summary>
        /// <param name="dbType"></param>
        /// <returns></returns>
        public override string GetConnectionString(Type dbType)
        {
            if (dbType == null) throw new ArgumentNullException(nameof(dbType));

            var connectionString = string.Empty;

            if (dbType == typeof(DefaultAppDbContext<IAppSlaveDb>))
            {
                if (_dbSetting.ReadConnectionStrings?.Any() == true)
                {
                    var index = new Random().Next(0, _dbSetting.ReadConnectionStrings.Count());
                    connectionString = _dbSetting.ReadConnectionStrings.ElementAt(index);
                }

                //如果读库连接不存在，则使用默认连接
                if (string.IsNullOrEmpty(connectionString))
                    connectionString = _dbSetting.DefaultConnectionString;
            }
            else
            {
                connectionString = _dbSetting.DefaultConnectionString;
            }

            return connectionString;
        }
    }
}