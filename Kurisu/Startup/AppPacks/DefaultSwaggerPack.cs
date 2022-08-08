using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Kurisu.Authentication;
using Kurisu.MVC;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Kurisu.Startup.AppPacks
{
    /// <summary>
    /// swagger默认pack
    /// </summary>
    public class DefaultSwaggerPack : BaseAppPack
    {
        public DefaultSwaggerPack()
        {
            Initialize();
        }

        public override int Order => 1;

        private static List<OpenApiInfo> _apiInfos;

        public override bool IsBeforeUseRouting => true;

        public override void ConfigureServices(IServiceCollection services)
        {
            var setting = Configuration.GetSection(nameof(SwaggerOAuthSetting)).Get<SwaggerOAuthSetting>();

            //eg:配置文件appsetting.json的key如果存在":"，那么解析将会失败
            var needFixeScopes = setting.Scopes.Where(x => x.Key.Contains("|")).ToDictionary(x => x.Key, x => x.Value);
            //移除 ｜ 相关key
            foreach (var scope in needFixeScopes)
            {
                setting.Scopes.Remove(scope.Key);
            }

            //修改正确的key，
            foreach (var scope in needFixeScopes)
            {
                var key = scope.Key.Replace("|", ":");
                setting.Scopes.TryAdd(key, scope.Value);
            }

            services.AddSwaggerGen(c =>
            {
                //没有分组的api
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "API文档",
                    Description = "当前为默认无分组API,各分组API接口说明(右上角切换)",
                });

                _apiInfos.ForEach(info => { c.SwaggerDoc(info.Title, info); });

                //api definition 切换(右上角下拉切换)
                c.DocInclusionPredicate((title, description) =>
                {
                    if (!description.TryGetMethodInfo(out MethodInfo method))
                        return false;

                    var apiInfo = method.DeclaringType?.GetCustomAttribute<ApiDefinitionAttribute>();
                    if (apiInfo != null)
                    {
                        return title == apiInfo.Title;
                    }

                    //没有分组的api
                    return title == "v1";
                });

                //加载xml注释文件
                Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.xml").ToList().ForEach(file => c.IncludeXmlComments(file));

                c.OperationFilter<AddResponseHeadersFilter>();
                c.OperationFilter<AppendAuthorizeToSummaryOperationFilter>();
                c.OperationFilter<SecurityRequirementsOperationFilter>();

                //OAuth2.0 Token 获取
                if (setting.Enable)
                {
                    c.OperationFilter<SwaggerOAuthOperationFilter>();
                    c.AddSecurityDefinition("identity.oauth2", new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.OAuth2,
                        In = ParameterLocation.Header,
                        Flows = new OpenApiOAuthFlows
                        {
                            AuthorizationCode = new OpenApiOAuthFlow
                            {
                                AuthorizationUrl = new Uri($"{setting.Authority}/connect/authorize"),
                                TokenUrl = new Uri($"{setting.Authority}/connect/token"),
                                Scopes = setting.Scopes,
                            }
                        }
                    });
                }

                //Bearer Token 
                c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
                {
                    Description = "请输入带有Bearer的Token，形如 “Bearer {Token}” ",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey
                });

                // c.CustomOperationIds(apiDesc =>
                // {
                //     var controllerAction = apiDesc.ActionDescriptor as ControllerActionDescriptor;
                //     return controllerAction.ControllerName + "-" + controllerAction.ActionName;
                // });
            });
        }

        public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "API文档");
                    _apiInfos.ForEach(info => { c.SwaggerEndpoint($"/swagger/{info.Title}/swagger.json", info.Title); });

                    //OAuth2.0 client 信息
                    var setting = Configuration.GetSection(nameof(SwaggerOAuthSetting)).Get<SwaggerOAuthSetting>();
                    c.OAuthClientId(setting.ClientId);
                    c.OAuthClientSecret(setting.ClientSecret);
                    c.OAuthUsePkce();
                });
            }
        }

        /// <summary>
        /// 初始化api info
        /// </summary>
        private static void Initialize()
        {
            var assembly = Assembly.GetEntryAssembly();
            var controllers = assembly.GetTypes().Where(x => x.IsAssignableTo(typeof(ControllerBase))
                                                             && x.IsDefined(typeof(ApiDefinitionAttribute)));

            _apiInfos = new List<OpenApiInfo>(controllers.Count());

            foreach (var controller in controllers)
            {
                var appInfo = controller.GetCustomAttribute<ApiDefinitionAttribute>();

                if (_apiInfos.All(x => x.Title != appInfo.Title))
                {
                    _apiInfos.Add(new OpenApiInfo
                    {
                        Title = appInfo.Title,
                        Version = "v1"
                    });
                }
            }
        }
    }

    /// <summary>
    /// 处理identity.oauth2过滤
    /// </summary>
    internal class SwaggerOAuthOperationFilter : IOperationFilter
    {
        private readonly SecurityRequirementsOperationFilter<AuthorizeAttribute> _filter;

        public SwaggerOAuthOperationFilter()
        {
            _filter = new SecurityRequirementsOperationFilter<AuthorizeAttribute>(attributes => from a in attributes where !string.IsNullOrEmpty(a.Policy) select a.Policy
                , true
                , "identity.oauth2");
        }

        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            _filter.Apply(operation, context);
        }
    }
}