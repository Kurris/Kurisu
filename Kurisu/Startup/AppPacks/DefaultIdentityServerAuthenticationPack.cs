using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Startup.AppPacks
{
    /// <summary>
    /// identityserver4默认pack
    /// </summary>
    public class DefaultIdentityServerAuthenticationPack : BaseAppPack
    {
        public override int Order => 2;
        
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