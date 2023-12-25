using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace Kurisu.Test.Framework.DI;

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

        var rootServices = services.BuildServiceProvider();

        //根服务提供器
        // = app.ApplicationServices;

        var type = Assembly.Load("Kurisu.AspNetCore").GetTypes().First(x => x.Name.Equals("InternalApp"));
        var obj = Activator.CreateInstance(type);
        var propertyInfo = obj.GetType().GetProperty("RootServices", BindingFlags.Static | BindingFlags.NonPublic);
        propertyInfo.SetValue(obj, rootServices);
    }
}