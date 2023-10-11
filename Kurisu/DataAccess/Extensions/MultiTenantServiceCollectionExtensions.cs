//using Kurisu.DataAccess;
//using Kurisu.DataAccess.Functions.Default.Abstractions;
//using Kurisu.DataAccess.Functions.Default.Internal;
//using Kurisu.DataAccess.Functions.MultiTenant;
//using Kurisu.DataAccess.Functions.MultiTenant.Resolvers;
//using Kurisu.DataAccess.Functions.UnitOfWork.Internal;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.DependencyInjection.Extensions;

//// ReSharper disable once CheckNamespace
//namespace Microsoft.Extensions.DependencyInjection;

///// <summary>
///// 数据库访问启用多租户扩展
///// </summary>
//[SkipScan]
//public static class MultiTenantServiceCollectionExtensions
//{
//    /// <summary>
//    /// 启用多租户
//    /// </summary>
//    /// <param name="builder"></param>
//    public static void EnableMultiTenantDiscriminator(this IKurisuDataAccessorBuilder builder)
//    {
//        builder.Services.AddKurisuTenantInfo();

//        //替换数据上下文保存
//        builder.Services.Replace(ServiceDescriptor.Singleton(typeof(IDefaultValuesOnSaveChangesResolver), typeof(MultiTenantDefaultValuesOnSaveChangesResolver)));

//        //租户查询过滤器
//        builder.Services.Replace(ServiceDescriptor.Singleton(typeof(IQueryFilterResolver), typeof(DefaultMultiTenantQueryFilterResolver)));

//        //替换为多租户数据库上下文
//        builder.Services.AddKurisuAppDbContext<MultiTenantWriteDbContext>();

//        var setting = new KurisuDataAccessorSettingBuilder();
//        builder.ConfigurationBuilders.ForEach(action => action(setting));

//        //替换主库
//        builder.Services.Replace(ServiceDescriptor.Scoped(typeof(IDbWrite), provider =>
//        {
//            var masterDbContext = provider.GetService<MultiTenantWriteDbContext>();
//            return new WriteImplementation(masterDbContext);
//        }));

//        //如果开启读写分离,替换IAppSlaveDb
//        if (setting.IsEnableReadWriteSplit)
//        {
//            builder.Services.AddKurisuAppDbContext<MultiTenantReadDbContext>();
//            builder.Services.Replace(ServiceDescriptor.Scoped(typeof(IDbRead), provider =>
//            {
//                var readDbContext = provider.GetRequiredService<MultiTenantReadDbContext>();
//                readDbContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
//                return new ReadImplementation(readDbContext);
//            }));
//        }

//        //如果开启工作单元,替换WriteImplementation
//        if (setting.IsEnableUnitOfWork)
//        {
//            builder.Services.AddKurisuAppDbContext<MultiTenantWriteInUnitOfWorkDbContext>();
//            builder.Services.Replace(ServiceDescriptor.Scoped(typeof(IDbWrite), provider =>
//            {
//                var writeDbContext = provider.GetRequiredService<MultiTenantWriteInUnitOfWorkDbContext>();
//                return new WriteInUnitOfWorkImplementation(writeDbContext);
//            }));
//        }

//        //配置开启多租户模式
//        builder.ConfigurationBuilders.Add(x => x.IsEnableMultiTenant = true);
//        builder.Services.Configure<KurisuDataAccessorSettingBuilder>(x => { builder.ConfigurationBuilders.ForEach(action => action(x)); });
//    }
//}