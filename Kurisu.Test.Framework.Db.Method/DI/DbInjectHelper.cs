using System;
using Kurisu.DataAccessor.Functions.Default.Abstractions;
using Kurisu.DataAccessor.Functions.Default.Internal;
using Kurisu.DataAccessor.Functions.ReadWriteSplit.Abstractions;
using Kurisu.DataAccessor.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kurisu.Test.Framework.Db.Method.DI;

/// <summary>
/// db 注入帮助类
/// </summary>
public class DbInjectHelper
{
    public static void InjectDbContext(IServiceCollection services)
    {
        services.AddDbContext<TestAppDbContext>((provider, options) =>
        {
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()).AddInterceptors(provider.GetService<DefaultDbCommandInterceptor>());
            //debug 启用日志
#if DEBUG
            options.EnableSensitiveDataLogging().EnableDetailedErrors();

            var loggerFactory = provider.GetService<ILoggerFactory>();
            options.UseLoggerFactory(loggerFactory);
#endif
        });

        //读写操作实现类
        services.AddScoped(typeof(IAppMasterDb), provider =>
        {
            var masterDbContext = provider.GetService<TestAppDbContext>();
            return new WriteImplementation(masterDbContext);
        });

        services.AddScoped<IAppDbService, DefaultAppDbService>();
    }
}