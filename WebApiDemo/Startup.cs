using Kurisu.Channel.Extensions;
using Kurisu.DataAccessor.Extensions;
using Kurisu.DataAccessor.Resolvers.Abstractions;
using Kurisu.Startup;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebApiDemo.Entities;

namespace WebApiDemo
{
    public class Startup : DefaultKurisuStartup
    {
        public Startup(IConfiguration configuration) : base(configuration)
        {
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddKurisuChannel();
            base.ConfigureServices(services);
            services.AddSingleton<IModelConfigurationSourceResolver, HereDefaultModelConfigurationSourceResolver>();

            //注入数据访问
            services.AddKurisuDatabaseAccessor()
                .AddKurisuUnitOfWork()
                .AddKurisuReadWriteSplit()
                .EnableMultiTenantDiscriminator();
        }

        public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            base.Configure(app, env);
        }
    }
}