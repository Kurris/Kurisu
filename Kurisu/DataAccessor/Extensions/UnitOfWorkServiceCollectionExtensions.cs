using System;
using System.Linq.Expressions;
using Kurisu.DataAccessor.Abstractions;
using Kurisu.DataAccessor.Internal;
using Kurisu.DataAccessor.UnitOfWork.Abstractions;
using Kurisu.DataAccessor.UnitOfWork.Internal;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class UnitOfWorkServiceCollectionExtensions
    {
        /// <summary>
        /// 添加工作单元
        /// </summary>
        /// <param name="services"></param>
        /// <param name="resolver"></param>
        /// <returns></returns>
        public static IServiceCollection AddKurisuUnitOfWork(this IServiceCollection services, Func<IServiceProvider, IUnitOfWorkDbContext> resolver)
        {
            //注册局部工作单元容器
            services.AddScoped<IDbContextContainer, DbContextContainer>();

            //工作单元DbContext获取
            services.AddSingleton(typeof(IUnitOfWorkDbContext), resolver);

            return services;
        }
    }
}