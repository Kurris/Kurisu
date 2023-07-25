using Kurisu.Grpc;
using Kurisu.Startup;
using Microsoft.IdentityModel.Logging;

namespace Kurisu.Test.WebApi_A;

public class Startup : DefaultKurisuStartup
{
    public Startup(IConfiguration configuration) : base(configuration)
    {
    }

    public override void ConfigureServices(IServiceCollection services)
    {
        base.ConfigureServices(services);
        services.AddGrpc(options =>
        {
            options.Interceptors.Add<ServiceLoggerInterceptor>();
            options.EnableDetailedErrors = true;
        });
    }

    public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        IdentityModelEventSource.ShowPII = true;
        base.Configure(app, env);
        app.UseEndpoints(builder => builder.MapGrpcServices());
    }
}