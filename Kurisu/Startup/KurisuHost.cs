using System.Threading.Tasks;
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
    public static void Run<TStartup>(bool useSerilog, string[] args) where TStartup : DefaultKurisuStartup
    {
        Host.CreateDefaultBuilder(args).RunKurisu<TStartup>(useSerilog);
    }

    /// <summary>
    /// 启动
    /// </summary>
    /// <param name="useSerilog"></param>
    /// <param name="args">参数</param>
    /// <typeparam name="TStartup">启动Startup类型</typeparam>
    public static async Task RunAsync<TStartup>(bool useSerilog, string[] args) where TStartup : DefaultKurisuStartup
    {
        await Host.CreateDefaultBuilder(args).RunKurisuAsync<TStartup>(useSerilog);
    }
}