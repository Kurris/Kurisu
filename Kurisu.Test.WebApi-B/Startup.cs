using Kurisu.AspNetCore.Authentication.Extensions;
using Kurisu.AspNetCore.Startup;

namespace Kurisu.Test.WebApi_B;

public class Startup : DefaultStartup
{
    public Startup(IConfiguration configuration) : base(configuration)
    {
    }

    public override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);
        services.AddUserInfo();
    }

    public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        base.Configure(app, env);
    }
}