using Kurisu.AspNetCore.DataAccess.SqlSugar.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;

namespace Kurisu.Test.DataAccess;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            //.AddJsonFile("appsettings.Development.json")
            .Build();

        services.AddConfiguration(configuration);
        services.AddSqlSugar(DbType.Sqlite);
    }
}