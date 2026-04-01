using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Kurisu.AspNetCore.Startup.Extensions;

/// <summary>
/// HostBuilder 扩展
/// </summary>
internal static class HostBuilderExtensions
{
    /// <summary>
    /// 配置 Serilog 日志
    /// </summary>
    /// <param name="hostBuilder"></param>
    /// <returns></returns>
    public static IHostBuilder ConfigSerilog(this IHostBuilder hostBuilder)
    {
        return hostBuilder.ConfigureLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddSerilog();
            })
            .UseSerilog((context, configuration) =>
                {
                    configuration.ReadFrom.Configuration(context.Configuration);
                }
            );
    }
}