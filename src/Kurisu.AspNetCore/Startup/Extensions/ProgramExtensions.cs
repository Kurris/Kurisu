using System.Threading.Tasks;
using Kurisu.AspNetCore.Abstractions.DependencyInjection;
using Kurisu.AspNetCore.Startup;
using Kurisu.AspNetCore.Startup.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

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
    public static void RunKurisu<TStartup>(this IHostBuilder hostBuilder) where TStartup : DefaultStartup
    {
        var host = hostBuilder.CreateHostBuild<TStartup>();
        host.Build().Run();
    }

    /// <summary>
    /// 运行kurisu framework
    /// </summary>
    /// <typeparam name="TStartup"></typeparam>
    /// <param name="hostBuilder"></param>
    /// <returns></returns>
    public static async Task RunKurisuAsync<TStartup>(this IHostBuilder hostBuilder) where TStartup : DefaultStartup
    {
        var host = hostBuilder.CreateHostBuild<TStartup>();
        await host.Build().RunAsync();
    }

    private static IHostBuilder CreateHostBuild<TStartup>(this IHostBuilder hostBuilder) where TStartup : DefaultStartup
    {
        var host = hostBuilder
            .ConfigSerilog()
            .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<TStartup>(); });

        return host;
    }
}