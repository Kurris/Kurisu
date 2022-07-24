using Kurisu.Startup;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Middleware;

namespace Kurisu.Gateway
{
    public class Startup : DefaultKurisuStartup
    {
        public Startup(IConfiguration configuration) : base(configuration)
        {
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);
            services.AddKurisuOcelot();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            base.Configure(app, env);
            app.UseOcelot().Wait();
        }
    }
}
