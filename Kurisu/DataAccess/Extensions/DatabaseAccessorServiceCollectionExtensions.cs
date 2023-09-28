using System;
using System.Collections.Generic;
using Kurisu.DataAccess;
using Kurisu.DataAccess.Functions.Default;
using Kurisu.DataAccess.Functions.Default.Abstractions;
using Kurisu.DataAccess.Functions.Default.Internal;
using Kurisu.DataAccess.Functions.Default.Resolvers;
using Kurisu.DataAccess.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 数据库访问扩展
/// </summary>
[SkipScan]
public static class DatabaseAccessorServiceCollectionExtensions
{
    /// <summary>
    /// 添加EFCore支持
    /// </summary>
    /// <param name="services"></param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <returns></returns>
    public static IKurisuDataAccessorBuilder AddKurisuDatabaseAccessor(this IServiceCollection services)
    {
        services.AddKurisuUserInfo();

        //数据库连接获取
        services.AddSingleton<IDbConnectStringResolver, DefaultDbConnectStringResolver>();
        //实体保存
        services.AddSingleton<IDefaultValuesOnSaveChangesResolver, DefaultValuesOnSaveChangesResolver>();
        //查询过滤
        services.AddSingleton<IQueryFilterResolver, DefaultQueryFilterResolver>();

        //defaultDbContext
        services.AddKurisuAppDbContext<DefaultAppDbContext<IDbWrite>>();

        //主库操作
        services.AddScoped(typeof(IDbWrite), provider =>
        {
            var dbWrite = provider.GetService<DefaultAppDbContext<IDbWrite>>();
            return new WriteImplementation(dbWrite);
        });

        //IDbService
        services.AddScoped<IDbService, DefaultAppDbService>();

        return new KurisuDataAccessorBuilder
        {
            Services = services,
            ConfigurationBuilders = new List<Action<KurisuDataAccessorSettingBuilder>>()
        };
    }


    /// <summary>
    /// 添加默认DbContext
    /// </summary>
    /// <param name="services"></param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="NullReferenceException"></exception>
    internal static void AddKurisuAppDbContext<TDbContext>(this IServiceCollection services) where TDbContext : DbContext
    {
        services.AddDbContext<TDbContext>((provider, dbContextOptionsBuilder) =>
        {
            var dbSetting = provider.GetService<IOptions<DbSetting>>()!.Value;
            var connectionString = provider.GetService<IDbConnectStringResolver>()!.GetConnectionString(typeof(TDbContext));

            dbContextOptionsBuilder.UseMySql(connectionString, MySqlServerVersion.LatestSupportedServerVersion, builder =>
                {
                    builder.CommandTimeout(dbSetting.Timeout);
                    //分段查询
                    //builder.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);

                    if (!string.IsNullOrEmpty(dbSetting.MigrationsAssembly))
                        builder.MigrationsAssembly(dbSetting.MigrationsAssembly);
                });
                //.ReplaceService<IModelCacheKeyFactory, DynamicModelCacheKeyFactory>()
                //.ReplaceService<IModelCustomizer, CustomModelCustomizer>()
                //.ReplaceService<IModelSource, CustomModelSource>();
                // .AddInterceptors(provider.GetRequiredService<IDbConnectionInterceptor>()
                //     , provider.GetRequiredService<IDbCommandInterceptor>()
                //     , provider.GetRequiredService<IDbTransactionInterceptor>()
                //     , provider.GetRequiredService<ISaveChangesInterceptor>());


            //debug 启用日志 , 这里的配置会被serilog覆盖
#if DEBUG
            dbContextOptionsBuilder.EnableSensitiveDataLogging().EnableDetailedErrors();

            var loggerFactory = provider.GetService<ILoggerFactory>();
            dbContextOptionsBuilder.UseLoggerFactory(loggerFactory);
#endif
        });
    }
}