using Kurisu.DataAccessor;
using Kurisu.DataAccessor.Abstractions.Setting;
using Kurisu.DataAccessor.Functions.Default.Abstractions;
using Kurisu.DataAccessor.Functions.Default.DbContexts;
using Kurisu.DataAccessor.Functions.Default.Internal;
using Kurisu.DataAccessor.Functions.ReadWriteSplit.Abstractions;
using Kurisu.DataAccessor.Functions.ReadWriteSplit.Internal;
using Kurisu.DataAccessor.Resolvers;
using Microsoft.EntityFrameworkCore;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// 数据库访问读写分离功能扩展类
    /// </summary>
    public static class ReadWriteSplitServiceCollectionExtensions
    {
        /// <summary>
        /// 启用读写分离
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IKurisuDataAccessorBuilder AddKurisuReadWriteSplit(this IKurisuDataAccessorBuilder builder)
        {
            //读写分离连接获取
            builder.Services.AddSingleton<IDbConnectStringResolver, DefaultReadWriteDbConnectStringResolver>();

            //增加从库获取
            builder.Services.AddKurisuAppDbContext<DefaultAppDbContext<IAppSlaveDb>>();

            builder.Services.AddScoped(typeof(IAppSlaveDb), provider =>
            {
                var slaveDbContext = provider.GetService<DefaultAppDbContext<IAppSlaveDb>>();
                slaveDbContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTrackingWithIdentityResolution;
                return new ReadImplementation(slaveDbContext);
            });

            //替换IAppDbService
            builder.Services.AddScoped<IAppDbService, ReadWriteSplitAppDbService>();

            //记录当前为读写分离
            builder.Services.Configure<KurisuDataAccessorBuilderSetting>(x => { x.IsReadWriteSplit = true; });

            return builder;
        }
    }
}