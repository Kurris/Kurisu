using System;
using System.Linq;
using Kurisu.DataAccessor;
using Kurisu.DataAccessor.Abstractions.Operation;
using Kurisu.DataAccessor.Abstractions.Setting;
using Kurisu.DataAccessor.DbContexts;
using Kurisu.DataAccessor.Interceptors;
using Kurisu.DataAccessor.Internal;
using Kurisu.DataAccessor.ReadWriteSplit.Abstractions;
using Kurisu.DataAccessor.Resolvers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// 数据库访问扩展
    /// </summary>
    public static class DatabaseAccessorServiceCollectionExtensions
    {
        /// <summary>
        /// 添加EFCore支持
        /// </summary>
        /// <param name="services"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <returns></returns>
        public static IServiceCollection AddKurisuDatabaseAccessor(this IServiceCollection services)
        {
            services.AddKurisuOptions<DbSetting>();

            services.TryAddSingleton<IDefaultValuesOnSaveChangesResolver, DefaultValuesOnSaveChangesResolver>();
            services.TryAddSingleton<IDefaultDbConnectStringResolver, DefaultDbConnectStringResolver>();
            services.TryAddSingleton<IDefaultQueryFilterResolver, DefaultQueryFilterResolver>();

            //dbContext
            services.AddDbContext();

            //主从库操作
            services.AddScoped(typeof(IAppMasterDb), provider => provider.GetService<Func<Type, IBaseDbService>>()?.Invoke(typeof(IAppMasterDb)));
            services.AddScoped(typeof(IAppSlaveDb), provider => provider.GetService<Func<Type, IBaseDbService>>()?.Invoke(typeof(IAppSlaveDb)));

            services.AddScoped<IAppDbService, AppDbService>();

            return services;
        }


        /// <summary>
        /// 添加默认DbContext
        /// </summary>
        /// <param name="services"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="NullReferenceException"></exception>
        private static void AddDbContext(this IServiceCollection services)
        {
            services.AddDbContext<DefaultAppDbContext>((provider, dbContextOptionsBuilder) =>
            {
                var dbSetting = provider.GetService<IOptions<DbSetting>>().Value;
                var connectionString = provider.GetService<IDefaultDbConnectStringResolver>().GetConnectionString(null);

                dbContextOptionsBuilder.UseMySql(connectionString, MySqlServerVersion.LatestSupportedServerVersion, builder =>
                {
                    builder.CommandTimeout(dbSetting.Timeout);

                    if (!string.IsNullOrEmpty(dbSetting.MigrationsAssembly))
                    {
                        builder.MigrationsAssembly(dbSetting.MigrationsAssembly);
                    }
                }).AddInterceptors(provider.GetService<DefaultDbCommandInterceptor>());

                //debug 启用日志
#if DEBUG
                dbContextOptionsBuilder.EnableSensitiveDataLogging().EnableDetailedErrors();

                var loggerFactory = provider.GetService<ILoggerFactory>();
                dbContextOptionsBuilder.UseLoggerFactory(loggerFactory);
#endif
            });

            // //读写操作实现类
            // services.AddScoped(provider =>
            // {
            //     return (Func<Type, IBaseDbService>) (dbType =>
            //     {
            //         //获取容器
            //         IBaseDbService implementation;
            //
            //         if (dbType == typeof(IAppMasterDb))
            //         {
            //             var masterDbContext = provider.GetService<DefaultAppDbContext<IAppMasterDb>>();
            //             implementation = new WriteImplementation(masterDbContext);
            //         }
            //         else
            //         {
            //             var dbSetting = provider.GetService<IOptions<DbSetting>>().Value;
            //             if (dbSetting.ReadConnectionStrings?.Any() == true)
            //             {
            //                 var slaveDbContext = provider.GetService<DefaultAppDbContext<IAppSlaveDb>>();
            //                 slaveDbContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTrackingWithIdentityResolution;
            //                 implementation = new ReadImplementation(slaveDbContext);
            //             }
            //             else
            //             {
            //                 implementation = null;
            //             }
            //         }
            //
            //         return implementation;
            //     });
            // });
        }
    }
}