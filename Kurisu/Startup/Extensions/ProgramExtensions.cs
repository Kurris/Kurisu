using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Kurisu.Serilog.Extensions;
using System;
using Kurisu.Startup;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class ProgramExtensions
    {
        /// <summary>
        /// 运行kurisu framework
        /// </summary>
        /// <param name="hostBuilder"></param>
        /// <param name="useGrpc">是否开启grpc</param>
        public static async Task RunKurisuAsync<TStartup>(this IHostBuilder hostBuilder, bool useGrpc = false) where TStartup : DefaultKurisuStartup
        {
            await hostBuilder.RunKurisuAsync(typeof(TStartup), useGrpc);
        }


        public static void RunKurisu<TStartup>(this IHostBuilder hostBuilder, bool useGrpc = false) where TStartup : DefaultKurisuStartup
        {
            hostBuilder.RunKurisu(typeof(TStartup), useGrpc);
        }

        /// <summary>
        /// 运行kurisu framework
        /// </summary>
        /// <param name="hostBuilder"></param>
        /// <returns></returns>
        public static async Task RunKurisuAsync(this IHostBuilder hostBuilder)
        {
            //TODO
            await hostBuilder.RunKurisuAsync(null, false);
        }

        /// <summary>
        /// 运行kurisu framework
        /// </summary>
        /// <param name="hostBuilder"></param>
        /// <param name="startup"></param>
        /// <param name="useGrpc"></param>
        /// <returns></returns>
        public static async Task RunKurisuAsync(this IHostBuilder hostBuilder, Type startup, bool useGrpc)
        {
            await hostBuilder.ConfigureLogging(builder =>
                {
                    builder.ClearProviders();
                    builder.AddSerilog();
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                        .UseStartup(startup);
                })
                .UseSerilogDefault()
                .Build()
                .RunAsync();
        }


        public static void RunKurisu(this IHostBuilder hostBuilder, Type startup, bool useGrpc)
        {
            hostBuilder.ConfigureLogging(builder =>
           {
               builder.ClearProviders();
               builder.AddSerilog();
           })
               .ConfigureWebHostDefaults(webBuilder =>
               {
                   webBuilder.UseStartup(startup);
               })
               .UseSerilogDefault()
               .Build()
               .Run();
        }
    }
}