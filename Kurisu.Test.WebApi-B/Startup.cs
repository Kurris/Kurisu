using Kurisu.Startup;

namespace Kurisu.Test.WebApi_B;

public class Startup : DefaultKurisuStartup
{
    public Startup(IConfiguration configuration) : base(configuration)
    {
    }

    public override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);
        services.AddKurisuUserInfo();
    }

    public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        base.Configure(app, env);
    }
}