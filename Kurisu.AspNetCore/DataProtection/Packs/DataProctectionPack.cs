using Kurisu.AspNetCore.Startup;
using Kurisu.AspNetCore.Utils;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Kurisu.AspNetCore.DataProtection.Packs;

/// <summary>
/// 数据保存pack
/// </summary>
public class DataProctectionPack : BaseAppPack
{
    public override bool IsBeforeUseRouting { get; }

    public override bool IsEnable
    {
        get
        {
            var setting = Configuration.GetSection(nameof(Settings.DataProtectionOptions)).Get<Settings.DataProtectionOptions>();
            return setting != null && setting.Enable;
        }
    }

    public override void ConfigureServices(IServiceCollection services)
    {
        var setting = Configuration.GetSection(nameof(Settings.DataProtectionOptions)).Get<Settings.DataProtectionOptions>();

        var reidConnection = Configuration.GetSection($"{nameof(RedisOptions)}:{nameof(RedisOptions.ConnectionString)}").Value;
        var redis = ConnectionMultiplexer.Connect(reidConnection);
        services.AddDataProtection()
            .SetApplicationName(setting.AppName)
            .PersistKeysToStackExchangeRedis(redis, setting.Key);
    }
}
