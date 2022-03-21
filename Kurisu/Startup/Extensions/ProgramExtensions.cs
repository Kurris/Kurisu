using System.Threading.Tasks;
using Kurisu.Startup.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Kurisu.Serilog.Extensions;
using Microsoft.AspNetCore.Builder;
using System;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Reflection;

namespace Kurisu.Startup.Extentsions
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

        /// <summary>
        /// 运行kurisu framework
        /// </summary>
        /// <param name="hostBuilder"></param>
        /// <param name="useGrpc"></param>
        /// <returns></returns>
        public static async Task RunKurisuAsync(this IHostBuilder hostBuilder, bool useGrpc = false)
        {
            //TODO
            await hostBuilder.RunKurisuAsync(null, useGrpc);
        }

        /// <summary>
        /// 运行kurisu framework
        /// </summary>
        /// <param name="hostBuilder"></param>
        /// <param name="startup"></param>
        /// <param name="useGrpc"></param>
        /// <returns></returns>
        public static async Task RunKurisuAsync(this IHostBuilder hostBuilder, Type startup, bool useGrpc = false)
        {
            await hostBuilder.ConfigureLogging(builder =>
                {
                    builder.ClearProviders();
                    builder.AddSerilog();
                }).ConfigureWebHostDefaults(webBuilder => { webBuilder.UseSerilogDefault().UseStartup(startup); })
                .Build()
                .RunAsync();
        }
    }
}