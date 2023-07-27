using Kurisu.DataAccessor;
using Kurisu.DataAccessor.Functions.Default.Abstractions;
using Kurisu.DataAccessor.Functions.Default.Internal;
using Kurisu.DataAccessor.Functions.MultiTenant.DbContexts;
using Kurisu.DataAccessor.Functions.MultiTenant.Resolvers;
using Kurisu.DataAccessor.Functions.ReadWriteSplit.Internal;
using Kurisu.DataAccessor.Functions.UnitOfWork.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 数据库访问启用多租户扩展
/// </summary>
[SkipScan]
public static class MultiTenantServiceCollectionExtensions
{
    /// <summary>
    /// 启用多租户
    /// </summary>
    /// <param name="builder"></param>
    public static void EnableMultiTenantDiscriminator(this IKurisuDataAccessorBuilder builder)
    {
        builder.Services.AddKurisuTenantInfo();

        //替换数据上下文保存
        //builder.Services.AddSingleton<IDefaultValuesOnSaveChangesResolver, MultiTenantDefaultValuesOnSaveChangesResolver>();
        builder.Services.Replace(ServiceDescriptor.Singleton(typeof(IDefaultValuesOnSaveChangesResolver), typeof(MultiTenantDefaultValuesOnSaveChangesResolver)));

        //租户查询过滤器
        //builder.Services.AddSingleton<IQueryFilterResolver, DefaultMultiTenantQueryFilterResolver>();
        builder.Services.Replace(ServiceDescriptor.Singleton(typeof(IQueryFilterResolver), typeof(DefaultMultiTenantQueryFilterResolver)));

        //替换为多租户数据库上下文
        builder.Services.AddKurisuAppDbContext<MultiTenantWriteDbContext>();

        var setting = new KurisuDataAccessorSettingBuilder();
        builder.ConfigurationBuilders.ForEach(action => action(setting));

        //替换主库
        //builder.Services.AddScoped(typeof(IAppMasterDb), provider =>
        //{
        //    var masterDbContext = provider.GetService<MultiTenantWriteDbContext>();
        //    return new WriteImplementation(masterDbContext);
        //});
        builder.Services.Replace(ServiceDescriptor.Scoped(typeof(IDbWrite), provider =>
        {
            var masterDbContext = provider.GetService<MultiTenantWriteDbContext>();
            return new WriteImplementation(masterDbContext);
        }));

        //如果开启读写分离,替换IAppSlaveDb
        if (setting.IsEnableReadWriteSplit)
        {
            builder.Services.AddKurisuAppDbContext<MultiTenantReadDbContext>();
            //builder.Services.AddScoped(typeof(IAppSlaveDb), provider =>
            //{
            //    var slaveDbContext = provider.GetService<MultiTenantReadDbContext>();
            //    slaveDbContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            //    return new ReadImplementation(slaveDbContext);
            //});
            builder.Services.Replace(ServiceDescriptor.Scoped(typeof(IDbRead), provider =>
            {
                var slaveDbContext = provider.GetService<MultiTenantReadDbContext>();
                slaveDbContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                return new ReadImplementation(slaveDbContext);
            }));
        }

        //如果开启工作单元,替换WriteImplementation
        if (setting.IsEnableUnitOfWork)
        {
            builder.Services.AddKurisuAppDbContext<MultiTenantWriteInUnitOfWorkDbContext>();
            //builder.Services.AddScoped(typeof(IAppMasterDb), provider =>
            //{
            //    var masterDbContext = provider.GetService<MultiTenantWriteInUnitOfWorkDbContext>();
            //    return new WriteInUnitOfWorkImplementation(masterDbContext);
            //});
            builder.Services.Replace(ServiceDescriptor.Scoped(typeof(IDbWrite), provider =>
            {
                var masterDbContext = provider.GetService<MultiTenantWriteInUnitOfWorkDbContext>();
                return new WriteInUnitOfWorkImplementation(masterDbContext);
            }));
        }

        //配置开启多租户模式
        builder.ConfigurationBuilders.Add(x => x.IsEnableMultiTenant = true);
        builder.Services.Configure<KurisuDataAccessorSettingBuilder>(x => { builder.ConfigurationBuilders.ForEach(action => action(x)); });
    }
}