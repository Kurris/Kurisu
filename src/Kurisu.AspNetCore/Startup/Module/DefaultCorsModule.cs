using Kurisu.AspNetCore.Abstractions.Startup;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.Startup.Module;

/// <summary>
/// cors默认pack
/// </summary>
public class DefaultCorsModule : AppModule
{
    public override string Name => "跨域模块";

    private const string Cors = "defaultCors";

    /// <inheritdoc />
    public override int Order => 1;

    /// <inheritdoc />
    public override void ConfigureServices(IServiceCollection services)
    {
        //添加跨域支持
        services.AddCors(options =>
        {
            options.AddPolicy(Cors, builder =>
            {
                builder.SetIsOriginAllowed(_ => true)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .DisallowCredentials();
            });
        });
    }

    /// <inheritdoc />
    public override void Configure(IApplicationBuilder app)
    {
        app.UseCors(Cors);
    }
}