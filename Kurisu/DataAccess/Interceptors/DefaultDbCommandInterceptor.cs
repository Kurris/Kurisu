//using System.Data.Common;
//using System.Threading;
//using System.Threading.Tasks;
//using Microsoft.EntityFrameworkCore.Diagnostics;
//using Microsoft.Extensions.Logging;
//using Microsoft.Extensions.Options;

//// ReSharper disable ClassNeverInstantiated.Global
//namespace Kurisu.DataAccess.Interceptors;

///// <summary>
///// SQL 命令执行查询器
///// </summary>
//public class DefaultDbCommandInterceptor : DbCommandInterceptor
//{
//    private readonly DbSetting _dbAppSetting;
//    private readonly ILogger<DefaultDbCommandInterceptor> _logger;

//    public DefaultDbCommandInterceptor(IOptions<DbSetting> options, ILogger<DefaultDbCommandInterceptor> logger)
//    {
//        _dbAppSetting = options.Value;
//        _logger = logger;
//    }

//    public override async ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
//    {
//        if (eventData.Duration.TotalMilliseconds >= _dbAppSetting.SlowSqlTime * 1000)
//        {
//            _logger.LogWarning("Duration:{Second}\r\nQuery:{Query}",eventData.Duration.TotalSeconds,command.CommandText);
//        }

//        return await base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
//    }

//    public override async ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
//    {
//        if (eventData.Duration.TotalMilliseconds >= _dbAppSetting.SlowSqlTime * 1000)
//        {
//            _logger.LogWarning("Duration:{Second}\r\nQuery:{Query}",eventData.Duration.TotalSeconds,command.CommandText);
//        }

//        return await base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
//    }

//    public override async ValueTask<object> ScalarExecutedAsync(DbCommand command, CommandExecutedEventData eventData, object result, CancellationToken cancellationToken = default)
//    {
//        if (eventData.Duration.TotalMilliseconds >= _dbAppSetting.SlowSqlTime * 1000)
//        {
//            _logger.LogWarning("Duration:{Second}\r\nQuery:{Query}",eventData.Duration.TotalSeconds,command.CommandText);
//        }

//        return await base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
//    }

//    public override async Task CommandFailedAsync(DbCommand command, CommandErrorEventData eventData, CancellationToken cancellationToken = default)
//    {
//        _logger.LogError("Exception:{Exception}\r\nQuery:{Query}",eventData.Exception.Message,command.CommandText);
//        await base.CommandFailedAsync(command, eventData, cancellationToken);
//    }
//}