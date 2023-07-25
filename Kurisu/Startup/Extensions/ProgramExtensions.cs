using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using Kurisu.Grpc;
using Kurisu.Startup;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;

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
    /// <param name="useSerilog"></param>
    public static async Task RunKurisuAsync<TStartup>(this IHostBuilder hostBuilder, bool useSerilog) where TStartup : DefaultKurisuStartup
    {
        await hostBuilder.RunKurisuAsync(typeof(TStartup), useSerilog);
    }

    /// <summary>
    /// 运行kurisu framework
    /// </summary>
    /// <param name="hostBuilder"></param>
    /// <param name="useSerilog"></param>
    /// <typeparam name="TStartup"></typeparam>
    public static void RunKurisu<TStartup>(this IHostBuilder hostBuilder, bool useSerilog) where TStartup : DefaultKurisuStartup
    {
        hostBuilder.RunKurisu(typeof(TStartup), useSerilog);
    }

    /// <summary>
    /// 运行kurisu framework
    /// </summary>
    /// <param name="hostBuilder"></param>
    /// <param name="startup"></param>
    /// <param name="useSerilog"></param>
    // ReSharper disable once MemberCanBePrivate.Global
    public static void RunKurisu(this IHostBuilder hostBuilder, Type startup, bool useSerilog)
    {
        RunKurisuAsync(hostBuilder, startup, useSerilog).GetAwaiter().GetResult();
    }


    /// <summary>
    /// 运行kurisu framework
    /// </summary>
    /// <param name="hostBuilder"></param>
    /// <param name="startup"></param>
    /// <param name="useSerilog"></param>
    /// <returns></returns>
    // ReSharper disable once MemberCanBePrivate.Global
    public static async Task RunKurisuAsync(this IHostBuilder hostBuilder, Type startup, bool useSerilog)
    {
        var host = hostBuilder.ConfigureLogging(builder =>
            {
                if (useSerilog)
                {
                    builder.ClearProviders();
                    builder.AddSerilog();
                }
            })
            .ConfigureWebHost(builder =>
            {
                builder.ConfigureKestrel((context, options) =>
                {
                    if (Environment.OSVersion.Platform == PlatformID.Unix)
                    {
                        var grpcSetting = context.Configuration.GetSection(nameof(GrpcSetting)).Get<GrpcSetting>();
                        if (grpcSetting?.Enable == true)
                        {
                            options.ListenAnyIP(grpcSetting.HttpPort, listenOptions => listenOptions.Protocols = HttpProtocols.Http1);
                            options.ListenAnyIP(grpcSetting.GrpcPort, listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
                        }
                    }
                });
            })
            .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup(startup); });

        if (useSerilog)
        {
            host.UseSerilog((context, configuration) => configuration.ReadFrom.Configuration(context.Configuration));
        }

        await host.Build()
            .RunAsync();
    }
}