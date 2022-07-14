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

            //注册局部工作单元容器
            services.AddScoped<IDbContextContainer, DbContextContainer>();
            services.AddDbConnections(action);

            //dbContext
            services.AddDbContext<IMasterDb>();
            services.AddDbContext<ISlaveDb>();

            //主库操作
            services.AddScoped(typeof(IMasterDb), provider => provider.GetService<Func<Type, IDbOperation>>()?.Invoke(typeof(IMasterDb)));
            services.AddScoped(typeof(ISlaveDb), provider => provider.GetService<Func<Type, IDbOperation>>()?.Invoke(typeof(ISlaveDb)));

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

                    if (dbType == typeof(IMasterDb))
                        connection.ConnectionString = dbSetting.DefaultConnectionString;
                    else
                    {
                        if (dbSetting.ReadConnectionStrings.Any())
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
        private static void AddDbContext<TIDb>(this IServiceCollection services) where TIDb : IDb
        {
            services.AddDbContext<AppDbContext<TIDb>>((provider, options) =>
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
                return (Func<Type, IDbOperation>) (dbType =>
                {
                    //获取容器
                    var container = provider.GetService<IDbContextContainer>();
                    IDbOperation implementation;

                    if (dbType == typeof(IMasterDb))
                    {
                        var masterDbContext = provider.GetService<AppDbContext<IMasterDb>>();
                        implementation = new WriteImplementation(masterDbContext);
                        container.Add(masterDbContext);
                    }
                    else
                    {
                        var slaveDbContext = provider.GetService<AppDbContext<ISlaveDb>>();
                        slaveDbContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTrackingWithIdentityResolution;
                        implementation = new ReadImplementation(slaveDbContext);
                    }

                    return implementation;
                });
            });
        }
    }
}