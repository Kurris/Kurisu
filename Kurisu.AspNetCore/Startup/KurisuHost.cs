using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Kurisu.AspNetCore.Startup;

/// <summary>
/// 启动Host
/// </summary>
[SkipScan]
public static class KurisuHost
{
    /// <summary>
    /// 启动
    /// </summary>
    /// <typeparam name="TStartup"></typeparam>
    /// <param name="args"></param>
    public static void Run<TStartup>(string[] args) where TStartup : DefaultStartup
    {
        Builder(args).RunKurisu<TStartup>();
    }

    /// <summary>
    /// 启动
    /// </summary>
    /// <param name="args">参数</param>
    /// <typeparam name="TStartup">启动Startup类型</typeparam>
    public static async Task RunAsync<TStartup>(string[] args) where TStartup : DefaultStartup
    {
        await Builder(args).RunKurisuAsync<TStartup>();
    }


    /// <summary>
    /// builder
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    public static IHostBuilder Builder(string[] args)
    {
        return Host.CreateDefaultBuilder(args);
    }
}