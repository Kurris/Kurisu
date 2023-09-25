using Kurisu.DataAccess;
using Kurisu.DataAccess.Functions.Default;
using Kurisu.DataAccess.Functions.Default.Abstractions;
using Kurisu.DataAccess.Functions.Default.Internal;
using Kurisu.DataAccess.Functions.ReadWriteSplit.Internal;
using Kurisu.DataAccess.Functions.ReadWriteSplit.Resolvers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 数据库访问读写分离功能扩展类
/// </summary>
[SkipScan]
public static class ReadWriteSplitServiceCollectionExtensions
{
    /// <summary>
    /// 启用读写分离
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static IKurisuDataAccessorBuilder AddKurisuReadWriteSplit(this IKurisuDataAccessorBuilder builder)
    {
        //替换读写分离连接
        builder.Services.Replace(ServiceDescriptor.Singleton(typeof(IDbConnectStringResolver), typeof(DefaultReadWriteDbConnectStringResolver)));

        //增加从库
        builder.Services.AddKurisuAppDbContext<DefaultAppDbContext<IDbRead>>();
        builder.Services.AddScoped(typeof(IDbRead), provider =>
        {
            var readDbContext = provider.GetRequiredService<DefaultAppDbContext<IDbRead>>();
            //取消EF跟踪行为
            readDbContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            return new ReadImplementation(readDbContext);
        });

        //替换IAppDbService
        builder.Services.Replace(ServiceDescriptor.Scoped(typeof(IDbService), typeof(ReadWriteSplitAppDbService)));

        //配置开启读写分离
        builder.ConfigurationBuilders.Add(x => x.IsEnableReadWriteSplit = true);
        builder.Services.Configure<KurisuDataAccessorSettingBuilder>(x => { builder.ConfigurationBuilders.ForEach(action => action(x)); });

        return builder;
    }
}