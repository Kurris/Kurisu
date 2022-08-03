using System;
using System.Linq;
using Kurisu.DataAccessor;
using Kurisu.DataAccessor.Abstractions;
using Kurisu.DataAccessor.Interceptors;
using Kurisu.DataAccessor.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Kurisu.Test.Db.DI
{
    /// <summary>
    /// db 注入帮助类
    /// </summary>
    public class DbInjectHelper
    {
        public static void InjectDbContext<TIDb>(IServiceCollection services) where TIDb : IDbService
        {
            services.AddDbContext<TestAppDbContext>((provider, options) =>
            {
                options.AddInterceptors(provider.GetService<DefaultDbCommandInterceptor>());
                options.UseInMemoryDatabase(Guid.NewGuid().ToString());
                //debug 启用日志
#if DEBUG
                options.EnableSensitiveDataLogging().EnableDetailedErrors();

                var loggerFactory = provider.GetService<ILoggerFactory>();
                options.UseLoggerFactory(loggerFactory);
#endif
            });

            //读写操作实现类
            services.AddScoped(provider =>
            {
                return (Func<Type, IDbService>) (dbType =>
                {
                    //获取容器
                    IDbService implementation;

                    if (dbType == typeof(IAppMasterDb))
                    {
                        var masterDbContext = provider.GetService<TestAppDbContext>();
                        implementation = new WriteImplementation(masterDbContext);
                    }
                    else
                    {
                        var dbSetting = provider.GetService<IOptions<DbSetting>>().Value;
                        if (dbSetting.ReadConnectionStrings?.Any() == true)
                        {
                            var slaveDbContext = provider.GetService<TestAppDbContext>();
                            slaveDbContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTrackingWithIdentityResolution;
                            implementation = new ReadImplementation(slaveDbContext);
                        }
                        else
                        {
                            implementation = null;
                        }
                    }

                    return implementation;
                });
            });
        }
    }
}