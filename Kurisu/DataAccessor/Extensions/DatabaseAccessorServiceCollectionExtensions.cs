using System;
using System.Data.Common;
using System.Linq;
using Kurisu.DataAccessor;
using Kurisu.DataAccessor.Abstractions;
using Kurisu.DataAccessor.Interceptors;
using Kurisu.DataAccessor.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;

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
        /// <param name="action"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <returns></returns>
        public static IServiceCollection AddKurisuDatabaseAccessor(this IServiceCollection services, Action<DbSetting> action = null)
        {
            services.AddKurisuOptions<DbSetting>();
            services.AddDbConnections(action);

            //dbContext
            services.AddDbContext<IAppMasterDb>();
            services.AddDbContext<IAppSlaveDb>();

            //主从库操作
            services.AddScoped(typeof(IAppMasterDb), provider => provider.GetService<Func<Type, IDbService>>()?.Invoke(typeof(IAppMasterDb)));
            services.AddScoped(typeof(IAppSlaveDb), provider => provider.GetService<Func<Type, IDbService>>()?.Invoke(typeof(IAppSlaveDb)));

            //读写分离操作
            services.AddScoped<IAppDbService, AppDbService>();

            return services;
        }

        /// <summary>
        /// 添加工作单元
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddKurisuUnitOfWork(this IServiceCollection services)
        {
            //注册局部工作单元容器
            services.AddScoped<IDbContextContainer, DbContextContainer>();
            return services;
        }


        /// <summary>
        /// 添加连接字符串
        /// </summary>
        /// <param name="services"></param>
        /// <param name="action"></param>
        /// <returns></returns>
        private static IServiceCollection AddDbConnections(this IServiceCollection services, Action<DbSetting> action = null)
        {
            //注册局部操作类型对应的连接
            services.AddScoped(provider =>
            {
                return (Func<Type, DbConnection>) (dbType =>
                {
                    var connection = new MySqlConnection();
                    var dbSetting = provider.GetService<IOptions<DbSetting>>()?.Value;
                    action?.Invoke(dbSetting);

                    if (dbType == typeof(IAppMasterDb))
                        connection.ConnectionString = dbSetting.DefaultConnectionString;
                    else
                    {
                        if (dbSetting.ReadConnectionStrings?.Any() == true)
                        {
                            var index = new Random().Next(0, dbSetting.ReadConnectionStrings.Count() - 1);
                            connection.ConnectionString = dbSetting.ReadConnectionStrings.ElementAt(index);
                        }

                        //如果读库连接不存在，则使用默认连接
                        if (string.IsNullOrEmpty(connection.ConnectionString))
                            connection.ConnectionString = dbSetting.DefaultConnectionString;
                    }

                    return connection;
                });
            });

            return services;
        }


        /// <summary>
        /// 添加DbContext
        /// </summary>
        /// <param name="services"></param>
        /// <typeparam name="TIDb"></typeparam>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="NullReferenceException"></exception>
        private static void AddDbContext<TIDb>(this IServiceCollection services) where TIDb : IDbService
        {
            services.AddDbContext<DefaultAppDbContext<TIDb>>((provider, options) =>
            {
                var dbSetting = provider.GetService<IOptions<DbSetting>>()?.Value ?? throw new ArgumentNullException(nameof(DbSetting) + " in " + nameof(AddDbContext));

                options.AddInterceptors(provider.GetService<DefaultDbCommandInterceptor>());

                var connectionResolve = provider.GetService<Func<Type, DbConnection>>() ?? throw new NullReferenceException(nameof(DbConnection));
                var connection = connectionResolve.Invoke(typeof(TIDb));

                options.UseMySql(connection, MySqlServerVersion.LatestSupportedServerVersion, builder =>
                {
                    builder.CommandTimeout(dbSetting.Timeout);

                    if (!string.IsNullOrEmpty(dbSetting.MigrationsAssembly))
                    {
                        builder.MigrationsAssembly(dbSetting.MigrationsAssembly);
                    }
                });

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
                        var masterDbContext = provider.GetService<DefaultAppDbContext<IAppMasterDb>>();
                        implementation = new WriteImplementation(masterDbContext);
                    }
                    else
                    {
                        var dbSetting = provider.GetService<IOptions<DbSetting>>().Value;
                        if (dbSetting.ReadConnectionStrings?.Any() == true)
                        {
                            var slaveDbContext = provider.GetService<DefaultAppDbContext<IAppSlaveDb>>();
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