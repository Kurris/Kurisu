using Microsoft.AspNetCore.Connections;

namespace Kurisu.Test.WebApi_A.AutoReload;

public static class ConfigurationBuilderExtensions
{
    public static IHostBuilder EnableAppSettingsReload(this IHostBuilder hostBuilder)
    {
        return hostBuilder.ConfigureAppConfiguration((x, builder) =>
        {
            var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddJsonFile($"appsettings.{x.HostingEnvironment.EnvironmentName}.json")
            .Build();


            var options = configuration.GetSection("ReloadConfigOptions").Get<SignalROptions>();
            builder.Add(new SignalRConfigurationSource(options));
        });
    }
}
