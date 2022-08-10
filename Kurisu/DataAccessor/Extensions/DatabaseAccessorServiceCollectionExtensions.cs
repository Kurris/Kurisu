using System;
using Kurisu.Authentication.Abstractions;
using Kurisu.Authentication.Internal;
using Kurisu.DataAccessor;
using Kurisu.DataAccessor.Abstractions.Setting;
using Kurisu.DataAccessor.Functions.Default.Abstractions;
using Kurisu.DataAccessor.Functions.Default.DbContexts;
using Kurisu.DataAccessor.Functions.Default.Internal;
using Kurisu.DataAccessor.Functions.ReadWriteSplit.Abstractions;
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
        /// <exception cref="ArgumentNullException"></exception>
        /// <returns></returns>
        public static IKurisuDataAccessorBuilder AddKurisuDatabaseAccessor(this IServiceCollection services)
        {
            services.TryAddSingleton<ICurrentUserInfoResolver, DefaultCurrentUserInfoResolver>();

            services.AddSingleton<IDbConnectStringResolver, DefaultDbConnectStringResolver>();
            services.AddSingleton<IDefaultValuesOnSaveChangesResolver, DefaultValuesOnSaveChangesResolver>();
            services.AddSingleton<IQueryFilterResolver, DefaultQueryFilterResolver>();

            //defualtDbContext
            services.AddKurisuAppDbContext<DefaultAppDbContext<IAppMasterDb>>();

            //主库操作
            services.AddScoped(typeof(IAppMasterDb), provider =>
            {
                var masterDbContext = provider.GetService<DefaultAppDbContext<IAppMasterDb>>();
                return new WriteImplementation(masterDbContext);
            });

            //IAppDbService
            services.AddScoped<IAppDbService, DefaultAppDbService>();

            return new KurisuDataAccessorBuilder
            {
                Services = services
            };
        }


        /// <summary>
        /// 添加默认DbContext
        /// </summary>
        /// <param name="services"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="NullReferenceException"></exception>
        internal static void AddKurisuAppDbContext<TDbContext>(this IServiceCollection services) where TDbContext : DbContext
        {
            services.AddDbContext<TDbContext>((provider, dbContextOptionsBuilder) =>
            {
                var dbSetting = provider.GetService<IOptions<DbSetting>>().Value;
                var connectionString = provider.GetService<IDbConnectStringResolver>().GetConnectionString(typeof(TDbContext));

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

                //Console.WriteLine("{0}:{1}", typeof(TDbContext), connectionString);
#endif
            });
        }
    }
}