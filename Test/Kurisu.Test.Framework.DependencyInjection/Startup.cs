using System.Linq;
using System.Reflection;
using Kurisu.Extensions.SqlSugar.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;

namespace Kurisu.Test.Framework.DependencyInjection;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Development.json").Build();

        services.AddConfiguration(configuration);
        services.AddDependencyInjection();
        services.AddSqlSugar(DbType.MySql);
        var rootServices = services.BuildServiceProvider(true);
        
        var type = Assembly.Load("Kurisu.AspNetCore").GetTypes().First(x => x.Name.Equals("InternalApp"));
        var propertyInfo = type.GetProperty("RootServices", BindingFlags.Static | BindingFlags.NonPublic)!;
        propertyInfo.SetValue(null, rootServices);
    }
}