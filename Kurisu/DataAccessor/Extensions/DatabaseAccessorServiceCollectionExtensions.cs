using System;
using System.Linq;
using Kurisu.DataAccessor;
using Kurisu.DataAccessor.Abstractions.Setting;
using Kurisu.DataAccessor.Functions.Default.Abstractions;
using Kurisu.DataAccessor.Functions.Default.DbContexts;
using Kurisu.DataAccessor.Functions.Default.Internal;
using Kurisu.DataAccessor.Functions.ReadWriteSplit.Abstractions;
using Kurisu.DataAccessor.Functions.ReadWriteSplit.DbContexts;
using Kurisu.DataAccessor.Interceptors;
using Kurisu.DataAccessor.Resolvers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
            services.TryAddSingleton<IDbConnectStringResolver, DefaultDbConnectStringResolver>();
            services.TryAddSingleton<IQueryFilterResolver, DefaultQueryFilterResolver>();

            //dbContext
            services.AddKurisuContext<DefaultAppDbContext>();
            //主库操作
            services.AddScoped(typeof(IAppMasterDb), provider => provider.GetService<Func<Type, IBaseDbService>>()?.Invoke(typeof(IAppMasterDb)));
            //IAppDbService
            services.AddScoped<IAppDbService, DefaultAppDbService>();

            return services;
        }


        /// <summary>
        /// 添加默认DbContext
        /// </summary>
        /// <param name="services"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="NullReferenceException"></exception>
        public static void AddKurisuContext<TDbContext>(this IServiceCollection services) where TDbContext : DbContext
        {
            services.AddDbContext<DefaultAppDbContext>((provider, dbContextOptionsBuilder) =>
            {
                var dbSetting = provider.GetService<IOptions<DbSetting>>().Value;
                var connectionString = provider.GetService<IDbConnectStringResolver>().GetConnectionString(null);

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
        }


        public static void AA(this IServiceCollection services)
        {
            //读写操作实现类
            services.AddScoped(provider =>
            {
                return (Func<Type, IBaseDbService>)(dbType =>
                {
                    //获取容器
                    IBaseDbService dbService;

                    if (dbType == typeof(IAppMasterDb))
                    {
                        var masterDbContext = provider.GetService<WriteAppDbContext>();
                        dbService = new WriteImplementation(masterDbContext);
                    }
                    else
                    {
                        var slaveDbContext = provider.GetService<ReadAppDbContext>();
                        slaveDbContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTrackingWithIdentityResolution;
                        dbService = new ReadImplementation(slaveDbContext);
                    }

                    return dbService;
                });
            });
        }
    }
}