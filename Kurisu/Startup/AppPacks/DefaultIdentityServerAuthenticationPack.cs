using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Startup.AppPacks
{
    public class DefaultIdentityServerAuthenticationPack : BaseAppPack
    {
        public override bool IsBeforeUseRouting => false;

        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddKurisuOAuth2Authentication();
        }

        public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseAuthentication();
            app.UseAuthorization();
        }
    }
}