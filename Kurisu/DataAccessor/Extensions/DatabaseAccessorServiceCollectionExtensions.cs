using System;
using Kurisu.Authentication.Abstractions;
using Kurisu.Authentication.Internal;
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
        /// <param name="options"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <returns></returns>
        public static IServiceCollection AddKurisuDatabaseAccessor(this IServiceCollection services, Action<DbSetting> options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            services.Configure(options);

            services.TryAddSingleton<ICurrentUserInfoResolver, DefaultCurrentUserInfoResolver>();
            services.AddSingleton<IDefaultValuesOnSaveChangesResolver, DefaultValuesOnSaveChangesResolver>();
            services.AddSingleton<IDbConnectStringResolver, DefaultDbConnectStringResolver>();
            services.AddSingleton<IQueryFilterResolver, DefaultQueryFilterResolver>();

            //dbContext
            services.AddKurisuContext<DefaultAppDbContext>();
            //主库操作
            services.AddScoped(typeof(IAppMasterDb), provider =>
            {
                var masterDbContext = provider.GetService<DefaultAppDbContext>();
                return new WriteImplementation(masterDbContext);
            });
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
            services.AddDbContext<TDbContext>((provider, dbContextOptionsBuilder) =>
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
    }
}