using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Kurisu;
using Kurisu.Authorization.Extensions;
using Kurisu.ConfigurableOptions.Extensions;
using Kurisu.Cors;
using Kurisu.Cors.Extensions;
using Kurisu.DependencyInjection.Extensions;
using Kurisu.ObjectMapper;
using Kurisu.ObjectMapper.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using TestApi.Services;

namespace TestApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            App.Configuration = Configuration;
            services.AddControllers();
            // services.AddControllers().AddNewtonsoftJson(options =>
            // {
            //     options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            // } );
            services.AddSwaggerGen(c => { c.SwaggerDoc("v1", new OpenApiInfo { Title = "TestApi", Version = "v1" }); });

            // services.AddCorsPolicy();
            services.AddObjectMapper(Assembly.GetExecutingAssembly());

            //     services.AddCorsPolicy();
            var cors = Configuration.GetSection(nameof(CorsAppSetting));
            ChangeToken.OnChange(Configuration.GetReloadToken,
                () => { Console.WriteLine("文件改变:" + cors["PolicyName"]); });
        
            services.AddAllConfigurationWithConfigurationAttribute();

            services.AddAppAuthentication();
            services.AddDependencyInjectionService();
            // services.AddTransient<IGeneratic<GenService>,GenService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            App.ServiceProvider = app.ApplicationServices;
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "TestApi v1");
                    c.RoutePrefix = string.Empty;
                });
            }

            //    app.UseCorsPolicy();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}