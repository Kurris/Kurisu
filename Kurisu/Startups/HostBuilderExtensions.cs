using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Kurisu.Startups;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kurisu.Startups
{
    public static class HostBuilderExtensions
    {
        public static IHostBuilder AddKurisu(this IHostBuilder builder)
        {
            builder.ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<KurisuStartup>();
            });
            return builder;
        }
    }
}