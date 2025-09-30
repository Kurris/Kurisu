using System;
using Kurisu.AspNetCore.Cache.Options;
using Kurisu.AspNetCore.DataProtection.Extensions;
using Kurisu.AspNetCore.Startup;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Kurisu.AspNetCore.DataProtection.Packs;

/// <summary>
/// 数据保护pack
/// </summary>
public class DataProtectionPack : BaseAppPack
{
    /// <inheritdoc />
    public override bool IsEnable => App.StartupOptions.DataProtectionOptions.Enable;

    /// <summary>
    /// Microsoft.AspNetCore.DataProtection.StackExchangeRedis
    /// </summary>
    /// <param name="services"></param>
    public override void ConfigureServices(IServiceCollection services)
    {
        var options = App.StartupOptions.DataProtectionOptions;

        var builder = services.AddDataProtection().SetApplicationName(options.AppName);

        if (options.Provider == DataProtectionProviderType.Redis)
        {
            var reidConnectionString = Configuration.GetSection($"{nameof(RedisOptions)}:{nameof(RedisOptions.ConnectionString)}").Value;
            var redisConnection = ConnectionMultiplexer.Connect(reidConnectionString);
            //DataProtection-Keys
            builder.PersistKeysToStackExchangeRedis(redisConnection);
        }
        else if (options.Provider == DataProtectionProviderType.Db)
        {
            builder.PersistKeysToDb();
        }
        else
        {
            throw new NotSupportedException(nameof(options.Provider));
        }
    }
}