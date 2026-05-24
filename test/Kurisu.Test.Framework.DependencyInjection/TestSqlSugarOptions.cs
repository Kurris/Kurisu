using System.IO;
using System.Text;
using AspectCore.Extensions.DependencyInjection;
using Kurisu.AspNetCore.Abstractions.DataAccess.Core;
using Kurisu.Extensions.SqlSugar;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;
using Xunit;

namespace Kurisu.Test.Framework.DependencyInjection;

[Trait("sqlsugar", "options")]
public class TestSqlSugarOptions
{
    [Fact]
    public void ResolveConnectionRegistry_WithEmptyAdditionalConnectionStrings_DoesNotThrow()
    {
        const string json = """
                            {
                              "DbOptions": {
                                "DbType": "MySqlConnector",
                                "DefaultConnectionString": "server=127.0.0.1;port=3306;userid=root;password=123456;database=test;charset=utf8mb4;",
                                "AdditionalConnectionStrings": {},
                                "Timeout": 30,
                                "SlowSqlTime": 1,
                                "EnableSqlLog": false,
                                "Generate": false
                              }
                            }
                            """;

        var services = new ServiceCollection();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var configuration = new ConfigurationBuilder()
            .AddJsonStream(stream)
            .Build();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddConfiguration(configuration);
        services.AddDependencyInjection();
        services.AddSqlSugar(DbType.MySqlConnector);

        var provider = services.BuildDynamicProxyProvider();
        var registry = provider.GetRequiredService<IDbConnectionRegistry>();

        Assert.Equal(
            "server=127.0.0.1;port=3306;userid=root;password=123456;database=test;charset=utf8mb4;",
            registry.GetConnectionString("DefaultConnectionString"));
    }
}