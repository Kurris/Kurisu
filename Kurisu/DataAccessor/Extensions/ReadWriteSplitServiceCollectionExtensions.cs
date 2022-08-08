using Kurisu.DataAccessor.Abstractions.Setting;
using Kurisu.DataAccessor.Functions.Default.Abstractions;
using Kurisu.DataAccessor.Functions.Default.Internal;
using Kurisu.DataAccessor.Functions.ReadWriteSplit.Abstractions;
using Kurisu.DataAccessor.Functions.ReadWriteSplit.DbContexts;
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
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddKurisuReadWriteSplit(this IServiceCollection services)
        {
            services.AddSingleton<IDbConnectStringResolver, DefaultReadWriteDbConnectStringResolver>();
            services.AddKurisuContext<ReadAppDbContext>();
            services.AddKurisuContext<WriteAppDbContext>();

            services.AddScoped(typeof(IAppSlaveDb), provider =>
            {
                var slaveDbContext = provider.GetService<ReadAppDbContext>();
                slaveDbContext.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTrackingWithIdentityResolution;
                return new ReadImplementation(slaveDbContext);
            });

            services.AddScoped<IAppDbService, ReadWriteSplitAppDbService>();

            return services;
        }
    }
}