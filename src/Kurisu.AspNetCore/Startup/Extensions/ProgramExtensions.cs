using System.Threading.Tasks;
using Kurisu.AspNetCore.Abstractions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 程序启动扩展
/// </summary>
[SkipScan]
public static class ProgramExtensions
{
    /// <summary>
    /// 运行kurisu framework
    /// </summary>
    /// <param name="hostBuilder"></param>
    /// <typeparam name="TStartup"></typeparam>
    public static void RunKurisu<TStartup>(this IHostBuilder hostBuilder) where TStartup : class
    {
        hostBuilder.RunKurisuAsync<TStartup>().Wait();
    }

    /// <summary>
    /// 运行kurisu framework
    /// </summary>
    /// <typeparam name="TStartup"></typeparam>
    /// <param name="hostBuilder"></param>
    /// <returns></returns>
    public static async Task RunKurisuAsync<TStartup>(this IHostBuilder hostBuilder) where TStartup : class
    {
        var host = hostBuilder
            .ConfigureLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddSerilog();
            })
            .UseSerilog((context, configuration) => configuration.ReadFrom.Configuration(context.Configuration))
            .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<TStartup>(); });

        await host.Build().RunAsync();
    }
}