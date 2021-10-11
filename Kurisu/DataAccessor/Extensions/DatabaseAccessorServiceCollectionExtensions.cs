using Kurisu.DataAccessor.Abstractions;
using Kurisu.DataAccessor.UnitOfWork.Filters;
using Kurisu.MVC.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.DataAccessor.Extensions
{
    /// <summary>
    /// 数据库访问扩展
    /// </summary>
    public static class DatabaseAccessorServiceCollectionExtensions
    {
        public static IServiceCollection AddDatabaseAccessor(this IServiceCollection services)
        {
            //容器
            services.AddScoped<IDbContextContainer, DbContextContainer>();


            services.AddScoped(provider =>
            {
                var container= provider.GetService<IDbContextContainer>();
                container.Add();

            });
                
            //工作单元
            services.AddMvcFilter<UnitOfWorkFilter>();

            return services;
        }
    }
}