using System.IO;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Kurisu.AspNetCore.Startup.Extensions;

/// <summary>
/// HostBuilder 扩展
/// </summary>
internal static class HostBuilderExtensions
{
    /// <summary>
    /// 配置 Serilog 日志
    /// </summary>
    /// <param name="hostBuilder"></param>
    /// <returns></returns>
    public static IHostBuilder ConfigSerilog(this IHostBuilder hostBuilder)
    {
        return hostBuilder.ConfigureLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddSerilog();
            })
            .UseSerilog((context, configuration) =>
                {
                    var v = context.Configuration.GetSection("Serilog")
                        .GetSection("WriteTo:0")
                        .GetSection("Name")
                        .Value;
                    if (string.IsNullOrEmpty(v))
                    {
                        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(DefaultAppsettingsJson));
                        var defaultConfig = new ConfigurationBuilder().AddJsonStream(ms).Build();
                        configuration.ReadFrom.Configuration(defaultConfig);
                        App.Logger.LogWarning("Serilog 配置节未找到，已使用默认配置");
                    }
                    else
                    {
                        configuration.ReadFrom.Configuration(context.Configuration);
                    }
                }
            );
    }


    private const string DefaultAppsettingsJson = @"

{
""Serilog"": {
    ""MinimumLevel"": {
      ""Default"": ""Information"",
      ""Override"": {
        ""System"": ""Warning"",
        ""Microsoft"": ""Warning"",
        ""Microsoft.Hosting.Lifetime"": ""Information""
      }
    },
    ""WriteTo"": [
      {
        ""Name"": ""Console"",
        ""Args"": {
          ""outputTemplate"": ""[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}"",
           ""theme"": ""Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme::Code, Serilog.Sinks.Console""
        }
      }
    ]
  }
}

";
}