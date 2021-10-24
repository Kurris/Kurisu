using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Kurisu.DataAccessor.Interceptors
{
    /// <summary>
    /// SQL 命令执行查询器
    /// </summary>
    public class ProfilerInterceptor : DbCommandInterceptor
    {
        private readonly DbAppSetting _dbAppSetting;
        private readonly ILogger<ProfilerInterceptor> _logger;

        public ProfilerInterceptor(DbAppSetting dbAppSetting, ILogger<ProfilerInterceptor> logger)
        {
            _dbAppSetting = dbAppSetting;
            _logger = logger;
        }

        public override async ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            if (!(eventData.Duration.TotalMilliseconds >= _dbAppSetting.SlowSqlTime * 1000))
                return await base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);

            var log = $"耗时:{eventData.Duration.TotalSeconds}秒\r\n" +
                      $"语句:{command.CommandText}";
            _logger.LogWarning(log);

            return await base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
        }

        public override async ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
        {
            if (!(eventData.Duration.TotalMilliseconds >= _dbAppSetting.SlowSqlTime * 1000))
                return await base.ReaderExecutedAsync(command, eventData, result, cancellationToken);

            var log = $"耗时:{eventData.Duration.TotalSeconds}秒\r\n" +
                      $"语句:{command.CommandText}";
            _logger.LogWarning(log);

            return await base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
        }

        public override async ValueTask<object> ScalarExecutedAsync(DbCommand command, CommandExecutedEventData eventData, object result, CancellationToken cancellationToken = default)
        {
            if (!(eventData.Duration.TotalMilliseconds >= _dbAppSetting.SlowSqlTime * 1000))
                return await base.ScalarExecutedAsync(command, eventData, result, cancellationToken);

            var log = $"耗时:{eventData.Duration.TotalSeconds}秒\r\n" +
                      $"语句:{command.CommandText}";
            _logger.LogWarning(log);

            return await base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
        }

        public override async Task CommandFailedAsync(DbCommand command, CommandErrorEventData eventData, CancellationToken cancellationToken = default)
        {
            var log = $"异常:{eventData.Exception.Message}\r\n" +
                      $"语句:{command.CommandText}";

            _logger.LogError(log);
            await base.CommandFailedAsync(command, eventData, cancellationToken);
        }
    }
}