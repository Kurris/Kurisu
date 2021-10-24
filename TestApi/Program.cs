using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Kurisu.Serilog.Extensions;
using Kurisu.Startups;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace TestApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
             Host.CreateDefaultBuilder(args)
            //     .ConfigureLogging(builder =>
            //     {
            //         builder.ClearProviders();
            //         builder.AddSerilog();
            //     })
                .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>().UseSerilogDefault();
            }).Build().Run();
        }
    }
}