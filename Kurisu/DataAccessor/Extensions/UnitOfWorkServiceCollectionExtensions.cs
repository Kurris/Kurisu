using System;
using Kurisu.DataAccessor;
using Kurisu.DataAccessor.Functions.ReadWriteSplit.Abstractions;
using Kurisu.DataAccessor.Functions.UnitOfWork.Abstractions;
using Kurisu.DataAccessor.Functions.UnitOfWork.DbContexts;
using Kurisu.DataAccessor.Functions.UnitOfWork.Internal;
using Microsoft.EntityFrameworkCore;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// 数据库工作单元扩展
    /// </summary>
    [SkipScan]
    public static class UnitOfWorkServiceCollectionExtensions
    {
        /// <summary>
        /// 添加工作单元
        /// </summary>
        /// <param name="builder">数据访问builder</param>
        /// <param name="unitOfWorkDbContextResolver">工作单元db上下文对象获取处理</param>
        /// <returns></returns>
        public static IKurisuDataAccessorBuilder AddKurisuUnitOfWork(this IKurisuDataAccessorBuilder builder, Func<IServiceProvider, DbContext> unitOfWorkDbContextResolver = null)
        {
            //注册局部工作单元容器
            builder.Services.AddScoped<IDbContextContainer, DbContextContainer>();

            builder.Services.AddKurisuAppDbContext<UnitOfWorkDbContext>();

            //替换主实现
            builder.Services.AddScoped<IAppMasterDb>(provider =>
            {
                var masterDbContext = provider.GetService<UnitOfWorkDbContext>();
                return new WriteInUnitOfWorkImplementation(masterDbContext);
            });

            //工作单元DbContext获取
            unitOfWorkDbContextResolver ??= provider => provider.GetService<IAppMasterDb>().GetMasterDbContext();
            builder.Services.AddScoped(typeof(IUnitOfWorkDbContext), _ => unitOfWorkDbContextResolver);

            //配置开启工作单元
            builder.ConfigurationBuilders.Add(x => x.IsEnableUnitOfWork = true);
            builder.Services.Configure<KurisuDataAccessorBuilderSetting>(x => { builder.ConfigurationBuilders.ForEach(action => action(x)); });

            return builder;
        }
    }
}