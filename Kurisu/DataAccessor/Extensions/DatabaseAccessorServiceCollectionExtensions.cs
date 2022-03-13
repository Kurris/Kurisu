using System;
using System.Data.Common;
using Kurisu.DataAccessor.Abstractions;
using Kurisu.DataAccessor.Interceptors;
using Kurisu.DataAccessor.Internal;
using Kurisu.MVC.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;

namespace Kurisu.DataAccessor.Extensions
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
        public static IServiceCollection AddDatabaseAccessor(this IServiceCollection services)
        {
            //注册局部工作单元容器
            services.AddScoped<IDbContextContainer, DbContextContainer>();

            services.AddScoped(provider =>
            {
                return (Func<Type, DbConnection>)(dbType =>
                    {
                        var connection = new MySqlConnection();
                        var dbAppSetting = provider.GetService<IOptions<DbAppSetting>>()?.Value;

                        if (dbType == typeof(IMasterDbService))
                            connection.ConnectionString = dbAppSetting.DefaultConnectionString;
                        else
                        {
                            var index = new Random().Next(dbAppSetting.ReadConnectionStrings.Count);
                            connection.ConnectionString = dbAppSetting.ReadConnectionStrings[index];

                            if (string.IsNullOrEmpty(connection.ConnectionString))
                                connection.ConnectionString = dbAppSetting.DefaultConnectionString;
                        }

                        return connection;
                    });
            });

            AddDbContext<IMasterDbService>(services);
            AddDbContext<ISlaveDbService>(services);

            //主库操作
            services.AddTransient<IMasterDbService>(provider => provider.GetService<Func<Type, DbOperationImplementation>>()?.Invoke(typeof(IMasterDbService)));
            //从库操作
            services.AddTransient<ISlaveDbService>(provider => provider.GetService<Func<Type, DbOperationImplementation>>()?.Invoke(typeof(ISlaveDbService)));

            //工作单元
            // services.AddMvcFilter<UnitOfWorkFilter>();

            return services;
        }


        /// <summary>
        /// 添加DbContext
        /// </summary>
        /// <param name="services"></param>
        /// <typeparam name="TDbService"></typeparam>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="NullReferenceException"></exception>
        private static void AddDbContext<TDbService>(IServiceCollection services) where TDbService : IDbService
        {
            services.AddDbContext<AppDbContext<TDbService>>((provider, options) =>
            {
                var dbAppSetting = provider.GetService<IOptions<DbAppSetting>>()?.Value;
                if (dbAppSetting == null) throw new ArgumentNullException(nameof(DbAppSetting));

                var cmdInterceptor = provider.GetRequiredService<DefaultDbCommandInterceptor>();


                //new ConnectionProfilerInterceptor()  , new DbContextSaveChangesInterceptor()
                options.AddInterceptors(cmdInterceptor);

                //局部共享连接
                var connectionResolve = provider.GetService<Func<Type, DbConnection>>() ?? throw new NullReferenceException("connection");
                var connection = connectionResolve.Invoke(typeof(TDbService));
                options.UseMySql(connection, MySqlServerVersion.LatestSupportedServerVersion, builder =>
                {
                    builder.CommandTimeout(dbAppSetting.Timeout);
                    builder.MigrationsAssembly(dbAppSetting.MigrationsAssembly);
                });

                if (!App.Env.IsDevelopment()) return;

                options.EnableSensitiveDataLogging().EnableDetailedErrors();

                var loggerFactory = provider.GetService<ILoggerFactory>();
                options.UseLoggerFactory(loggerFactory);
            }, ServiceLifetime.Transient);

            services.AddTransient(provider =>
            {
                DbOperationImplementation DbOperationImplementationResolve(Type dbType)
                {
                    //获取上下文
                    DbContext dbContext;

                    if (dbType == typeof(IMasterDbService))
                        dbContext = provider.GetService<AppDbContext<IMasterDbService>>();
                    else
                    {
                        dbContext = provider.GetService<AppDbContext<ISlaveDbService>>();
                        dbContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTrackingWithIdentityResolution;
                    }

                    //获取容器
                    var container = provider.GetService<IDbContextContainer>();
                    container?.Add(dbContext);

                    return new MySqlDb(dbContext);
                }

                return (Func<Type, DbOperationImplementation>)DbOperationImplementationResolve;
            });
        }
    }
}