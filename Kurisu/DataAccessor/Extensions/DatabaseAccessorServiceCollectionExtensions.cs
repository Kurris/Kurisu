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
                options.UseMySql("", ServerVersion.Parse(""), builder =>
                {
                    builder.CommandTimeout(5);
                    // builder.MigrationsAssembly()
                });
                
                options.UseLoggerFactory(LoggerFactory.Create(builder =>
                {
                    builder.AddFilter((category, level) =>
                        category == DbLoggerCategory.Database.Command.Name
                        && level == LogLevel.Information).AddConsole();
                }));
            });

            //注册容器
            services.AddScoped<IDbContextContainer, DbContextContainer>();

            services.AddTransient<IMasterDbImplementation>(provider =>
            {
                DbContext dbContext = provider.GetService<AppDbContext>();
                //获取容器
                var container = provider.GetService<IDbContextContainer>();
                container?.Add(dbContext);

                return new MySqlDb(dbContext);
            });

            //工作单元
            services.AddMvcFilter<UnitOfWorkFilter>();

            return services;
        }
    }
}