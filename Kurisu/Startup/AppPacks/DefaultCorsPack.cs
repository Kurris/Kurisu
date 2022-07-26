using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Startup.AppPacks
{
    /// <summary>
    /// cors默认pack
    /// </summary>
    public class DefaultCorsPack : BaseAppPack
    {
        private readonly string _cors = "defaultCors";

        public override int Order => 1;

        public override bool IsBeforeUseRouting => false;

        public override void ConfigureServices(IServiceCollection services)
        {
            //添加跨域支持
            services.AddCors(options =>
            {
                options.AddPolicy(_cors, builder =>
                {
                    builder.SetIsOriginAllowed(_ => true)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });
        }

        public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseCors(_cors);
        }
    }
}