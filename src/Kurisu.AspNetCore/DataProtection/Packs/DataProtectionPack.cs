using Kurisu.AspNetCore.Cache.Options;
using Kurisu.AspNetCore.Startup;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Kurisu.AspNetCore.DataProtection.Packs;

/// <summary>
/// 数据保护pack
/// </summary>
public class DataProtectionPack : BaseAppPack
{
    /// <inheritdoc />
    public override bool IsEnable
    {
        get
        {
            var setting = Configuration.GetSection(nameof(Settings.DataProtectionOptions)).Get<Settings.DataProtectionOptions>();
            return setting is { Enable: true };
        }
    }

    /// <summary>
    /// Microsoft.AspNetCore.DataProtection.StackExchangeRedis
    /// </summary>
    /// <param name="services"></param>
    public override void ConfigureServices(IServiceCollection services)
    {
        var setting = Configuration.GetSection(nameof(Settings.DataProtectionOptions)).Get<Settings.DataProtectionOptions>();

        var reidConnectionString = Configuration.GetSection($"{nameof(RedisOptions)}:{nameof(RedisOptions.ConnectionString)}").Value;
        var redisConnection = ConnectionMultiplexer.Connect(reidConnectionString);
        services.AddDataProtection()
            .SetApplicationName(setting.AppName)
            .PersistKeysToStackExchangeRedis(redisConnection, setting.Key);
    }
}