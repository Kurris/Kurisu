using System;
using Kurisu.DataAccessor.Abstractions;
using Kurisu.DataAccessor.Interceptors;
using Kurisu.DataAccessor.Internal;
using Kurisu.DataAccessor.UnitOfWork.Filters;
using Kurisu.MVC.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Extensions.Logging;

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
            services.AddDbContext<AppDbContext>((provider, options) =>
            {
                var dbAppSetting = provider.GetService<IOptions<DbAppSetting>>()?.Value;
                if (dbAppSetting == null) throw new ArgumentNullException(nameof(DbAppSetting));

                var profileLogger = provider.GetService<ILogger<ProfilerInterceptor>>();

                options.AddInterceptors(new ConnectionProfilerInterceptor()
                    , new DbContextSaveChangesInterceptor()
                    , new ProfilerInterceptor(dbAppSetting, profileLogger));

                options.UseMySql(dbAppSetting.SqlConnectionString, ServerVersion.Parse(dbAppSetting.Version),
                    builder =>
                    {
                        builder.CommandTimeout(dbAppSetting.Timeout);
                        builder.MigrationsAssembly(dbAppSetting.MigrationsAssembly);
                    });

                if (App.IsDebug)
                {
                    var loggerFactory = provider.GetService<ILoggerFactory>();
                    // options.UseLoggerFactory(LoggerFactory.Create(builder =>
                    // {
                    //     builder.AddFilter((category, level) =>
                    //             category == DbLoggerCategory.Database.Command.Name
                    //             && level == LogLevel.Information)
                    //         .AddConsole();
                    // }));

                    options.UseLoggerFactory(loggerFactory);
                }
            });

            //注册局部工作单元容器
            services.AddScoped<IDbContextContainer, DbContextContainer>();

            //数据库操作实现类
            services.AddTransient<DbOperationImplementation>(provider =>
            {
                //获取上下文
                DbContext dbContext = provider.GetService<AppDbContext>();
                //获取容器
                var container = provider.GetService<IDbContextContainer>();
                container?.Add(dbContext);

                return new MySqlDb(dbContext);
            });

            //主库操作
            services.AddTransient<IMasterDbService>(provider => provider.GetService<DbOperationImplementation>());
            //从库操作
            services.AddTransient<ISlaveDbService>(provider => provider.GetService<DbOperationImplementation>());

            //工作单元
            services.AddMvcFilter<UnitOfWorkFilter>();

            return services;
        }
    }
}