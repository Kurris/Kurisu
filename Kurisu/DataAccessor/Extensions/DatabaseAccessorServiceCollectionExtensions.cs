using System;
using System.Collections.Generic;
using Kurisu.DataAccessor;
using Kurisu.DataAccessor.Functions.Default.Abstractions;
using Kurisu.DataAccessor.Functions.Default.DbContexts;
using Kurisu.DataAccessor.Functions.Default.Internal;
using Kurisu.DataAccessor.Functions.Default.Resolvers;
using Kurisu.DataAccessor.Interceptors;
using Microsoft.EntityFrameworkCore;
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
        //模型配置来源
        services.AddSingleton<IModelConfigurationSourceResolver, DefaultModelConfigurationSourceResolver>();

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
            Services = services
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
            })
            .ReplaceService<IModelCacheKeyFactory, CustomModelCacheKeyFactory>()
            .AddInterceptors(provider.GetService<DefaultDbCommandInterceptor>());

            //debug 启用日志 , 这里的配置会被serilog覆盖
#if DEBUG
            dbContextOptionsBuilder.EnableSensitiveDataLogging().EnableDetailedErrors();

            var loggerFactory = provider.GetService<ILoggerFactory>();
            dbContextOptionsBuilder.UseLoggerFactory(loggerFactory);

            //ConsoleDemo.WriteLine("{0}:{1}", typeof(TDbContext), connectionString);
#endif
        });
    }
}