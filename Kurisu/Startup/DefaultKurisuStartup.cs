﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Kurisu.Authorization.Extensions;
using Kurisu.Authorization.Middlewares;
using Kurisu.ConfigurableOptions.Extensions;
using Kurisu.Cors;
using Kurisu.DataAccessor.Extensions;
using Kurisu.DependencyInjection.Extensions;
using Kurisu.ObjectMapper.Extensions;
using Kurisu.Startup.Abstractions;
using Kurisu.UnifyResultAndValidation.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Kurisu.Startup
{
    /// <summary>
    /// 默认启动类
    /// </summary>
    public abstract class DefaultKurisuStartup : BasePack
    {
        private readonly IConfiguration _configuration;

        public DefaultKurisuStartup(IConfiguration configuration)
        {
            _configuration = configuration;
            App.Configuration = _configuration;
            _configuration = configuration;
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddControllers().AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            });


            services.AddSwaggerGen(c => { c.SwaggerDoc("v1", new OpenApiInfo { Title = "TestApi", Version = "v1" }); });
            services.AddObjectMapper(Assembly.GetExecutingAssembly());

            var cors = _configuration.GetSection(nameof(CorsAppSetting));
            ChangeToken.OnChange(_configuration.GetReloadToken,
                () => { Console.WriteLine("文件改变:" + cors["PolicyName"]); });

            services.AddAllConfigurationWithConfigurationAttribute();

            // services.AddJwtAuthentication();
            services.AddDependencyInjectionService();
            services.AddDatabaseAccessor();
            services.AddUnify();
        }

        public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            App.ServiceProvider = app.ApplicationServices;
            App.Env = env;
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

            // app.UseMiddleware<ExceptionMiddleware>();


            // app.UseAuthorization();
            app.UseRouting();
            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}