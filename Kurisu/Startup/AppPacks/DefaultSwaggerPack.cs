using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Kurisu.Document.Settings;
using Kurisu.MVC;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using Swashbuckle.AspNetCore.SwaggerGen;

// ReSharper disable ClassNeverInstantiated.Global

namespace Kurisu.Startup.AppPacks;

/// <summary>
/// swagger默认pack
/// </summary>
public class DefaultSwaggerPack : BaseAppPack
{
    private static List<OpenApiInfo> _apiInfos;

    public DefaultSwaggerPack()
    {
        Initialize();
    }

    public override int Order => 1;

    public override bool IsBeforeUseRouting => true;

    public override void ConfigureServices(IServiceCollection services)
    {
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
            c.DocInclusionPredicate((group, description) =>
            {
                if (!description.TryGetMethodInfo(out var method))
                    return false;

                var apiInfo = method.DeclaringType?.GetCustomAttribute<ApiDefinitionAttribute>();
                if (apiInfo != null)
                {
                    return group == apiInfo.Group;
                }

                //没有分组的api
                return group == "v1";
            });

            //加载xml注释文件
            Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.xml")
                .ToList()
                .ForEach(file => c.IncludeXmlComments(file));

            //Bearer Token
            c.AddSecurityRequirement(new OpenApiSecurityRequirement()
                {
                    {
                        new OpenApiSecurityScheme()
                        {
                             Reference = new OpenApiReference()
                             {
                                 Type = ReferenceType.SecurityScheme,
                                 Id = JwtBearerDefaults.AuthenticationScheme
                             }
                        },Array.Empty<string>()
                    }
                });
            c.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
            {
                Description = "请输入带有Bearer的Token,如“Bearer {Token}” ",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey
            });
            c.OperationFilter<AddResponseHeadersFilter>();
            c.OperationFilter<AppendAuthorizeToSummaryOperationFilter>();

            //OAuth2.0 Token 获取
            var setting = Configuration.GetSection(nameof(SwaggerOAuthSetting)).Get<SwaggerOAuthSetting>();
            if (setting != null && setting.Enable)
            {
                //eg:配置文件appsetting.json的key如果存在":"，那么解析将会失败
                var needFixeScopes = setting.Scopes
                    .Where(x => x.Key.Contains('|'))
                    .ToDictionary(x => x.Key, x => x.Value);

                //移除｜相关key
                foreach (var scope in needFixeScopes)
                    setting.Scopes.Remove(scope.Key);

                //修改正确的key，
                foreach (var (s, value) in needFixeScopes)
                {
                    var key = s.Replace("|", ":");
                    setting.Scopes.TryAdd(key, value);
                }

                c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
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

                c.OperationFilter<SecurityRequirementsOperationFilter>();
            }
        });
    }

    public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            //OAuth2.0 client 信息
            var setting = Configuration.GetSection(nameof(SwaggerOAuthSetting)).Get<SwaggerOAuthSetting>();
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "API文档");
                _apiInfos.ForEach(info => { c.SwaggerEndpoint($"/swagger/{info.Title}/swagger.json", info.Title); });

                if (setting?.Enable == true)
                {
                    c.OAuthClientId(setting.ClientId);
                    c.OAuthClientSecret(setting.ClientSecret);
                    c.OAuthUsePkce();
                }
            });
        }
    }

    /// <summary>
    /// 初始化api info
    /// </summary>
    private static void Initialize()
    {
        var assembly = Assembly.GetEntryAssembly()!;
        var controllers = assembly.GetTypes().Where(x =>
            x.IsAssignableTo(typeof(ControllerBase))
            && x.IsDefined(typeof(ApiDefinitionAttribute))).ToArray();

        _apiInfos = new List<OpenApiInfo>(controllers.Length);

        foreach (var controller in controllers)
        {
            var apiInfo = controller.GetCustomAttribute<ApiDefinitionAttribute>()!;

            if (_apiInfos.All(x => x.Title != apiInfo.Group))
            {
                _apiInfos.Add(new OpenApiInfo
                {
                    Title = apiInfo.Group,
                    Version = "v1",
                    Description = apiInfo.Description
                });
            }
        }
    }
}