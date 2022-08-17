using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using Kurisu;
using Kurisu.Grpc;
using Kurisu.Startup;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
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
        public static async Task RunKurisuAsync<TStartup>(this IHostBuilder hostBuilder) where TStartup : DefaultKurisuStartup
        {
            await hostBuilder.RunKurisuAsync(typeof(TStartup));
        }

        /// <summary>
        /// 运行kurisu framework
        /// </summary>
        /// <param name="hostBuilder"></param>
        /// <typeparam name="TStartup"></typeparam>
        public static void RunKurisu<TStartup>(this IHostBuilder hostBuilder) where TStartup : DefaultKurisuStartup
        {
            hostBuilder.RunKurisu(typeof(TStartup));
        }

        /// <summary>
        /// 运行kurisu framework
        /// </summary>
        /// <param name="hostBuilder"></param>
        /// <param name="startup"></param>
        /// <returns></returns>
        // ReSharper disable once MemberCanBePrivate.Global
        public static async Task RunKurisuAsync(this IHostBuilder hostBuilder, Type startup)
        {
            await hostBuilder.ConfigureLogging(builder =>
                {
                    builder.ClearProviders();
                    builder.AddSerilog();
                })
                .ConfigureWebHostDefaults((webBuilder) =>
                {
                    webBuilder.ConfigureKestrel((context, options) =>
                    {
                        var grpcSetting = context.Configuration.GetSection(nameof(GrpcSetting)).Get<GrpcSetting>();

                        if (grpcSetting.Enable)
                        {
                            options.ListenAnyIP(grpcSetting.HttpPort, listenOptions => listenOptions.Protocols = HttpProtocols.Http1);
                            options.ListenAnyIP(grpcSetting.GrpcPort, listenOptions => listenOptions.Protocols = HttpProtocols.Http2);
                        }
                    });
                    webBuilder.UseStartup(startup);
                })
                .UseSerilog((context, configuration) => configuration.ReadFrom.Configuration(context.Configuration))
                .Build()
                .RunAsync();
        }

        /// <summary>
        /// 运行kurisu framework
        /// </summary>
        /// <param name="hostBuilder"></param>
        /// <param name="startup"></param>
        /// <param name="useGrpc"></param>
        // ReSharper disable once MemberCanBePrivate.Global
        public static void RunKurisu(this IHostBuilder hostBuilder, Type startup)
        {
            RunKurisuAsync(hostBuilder, startup).GetAwaiter().GetResult();
        }
    }
}