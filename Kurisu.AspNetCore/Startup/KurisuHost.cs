using System.Threading.Tasks;
using Kurisu.AspNetCore.Startup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Kurisu.Startup;

/// <summary>
/// 启动Host
/// </summary>
public static class KurisuHost
{
    /// <summary>
    /// 启动
    /// </summary>
    /// <param name="useSerilog"></param>
    /// <param name="args">参数</param>
    /// <typeparam name="TStartup">>启动Startup类型</typeparam>
    public static void Run<TStartup>(bool useSerilog, string[] args) where TStartup : DefaultStartup
    {
        Host.CreateDefaultBuilder(args).RunKurisu<TStartup>(useSerilog);
    }

    /// <summary>
    /// 启动
    /// </summary>
    /// <typeparam name="TStartup"></typeparam>
    /// <param name="args"></param>
    public static void Run<TStartup>(string[] args) where TStartup : DefaultStartup
    {
        Host.CreateDefaultBuilder(args).RunKurisu<TStartup>(true);
    }

    /// <summary>
    /// 启动
    /// </summary>
    /// <param name="useSerilog"></param>
    /// <param name="args">参数</param>
    /// <typeparam name="TStartup">启动Startup类型</typeparam>
    public static async Task RunAsync<TStartup>(bool useSerilog, string[] args) where TStartup : DefaultStartup
    {
        await Host.CreateDefaultBuilder(args).RunKurisuAsync<TStartup>(useSerilog);
    }

    /// <summary>
    /// 启动
    /// </summary>
    /// <param name="args">参数</param>
    /// <typeparam name="TStartup">启动Startup类型</typeparam>
    public static async Task RunAsync<TStartup>(string[] args) where TStartup : DefaultStartup
    {
        await Host.CreateDefaultBuilder(args).RunKurisuAsync<TStartup>(true);
    }
}