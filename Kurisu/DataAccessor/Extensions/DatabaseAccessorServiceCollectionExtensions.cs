using System.Diagnostics;
using Kurisu.DataAccessor.Abstractions;
using Kurisu.DataAccessor.Interceptors;
using Kurisu.DataAccessor.Internal;
using Kurisu.DataAccessor.UnitOfWork.Filters;
using Kurisu.MVC.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
        /// <returns></returns>
        public static IServiceCollection AddDatabaseAccessor(this IServiceCollection services)
        {
            services.AddDbContext<AppDbContext>(options =>
            {
                options.AddInterceptors(new ConnectionProfilerInterceptor(), new DbContextSaveChangesInterceptor(), new ProfilerInterceptor());
                options.UseMySql("data source=localhost;database=test; uid=root;pwd=123456;", ServerVersion.Parse("8.0.0"), builder =>
                {
                    builder.CommandTimeout(5);
                    builder.MigrationsAssembly("TestApi");
                });

                options.UseLoggerFactory(LoggerFactory.Create(builder =>
                {
                    builder.AddFilter((category, level) =>
                        category == DbLoggerCategory.Database.Command.Name
                        && level == LogLevel.Information).AddConsole();
                }));
            });

            //注册局部容器
            services.AddScoped<IDbContextContainer, DbContextContainer>();

            services.AddTransient<DbOperationImplementation>(provider =>
            {
                DbContext dbContext = provider.GetService<AppDbContext>();
                //获取容器
                var container = provider.GetService<IDbContextContainer>();
                container?.Add(dbContext);

                var db = new MySqlDb(dbContext);
                return db;
            });

            services.AddTransient<IMasterDbImplementation>(provider => provider.GetService<DbOperationImplementation>());
            services.AddTransient<ISlaveDbImplementation>(provider =>
            {
                var db = provider.GetService<DbOperationImplementation>();
                if (db != null)
                {
                    db.AsNoTrackingWithIdentityResolution();
                    return db;
                }

                return null;
            });

            //工作单元
            services.AddMvcFilter<UnitOfWorkFilter>();

            return services;
        }
    }
}