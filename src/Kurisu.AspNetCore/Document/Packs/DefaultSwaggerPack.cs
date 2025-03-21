using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Kurisu.AspNetCore.Document.Settings;
using Kurisu.AspNetCore.MVC;
using Kurisu.AspNetCore.Startup;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Kurisu.AspNetCore.Document.Packs;

/// <summary>
/// swagger默认pack
/// </summary>
public class DefaultSwaggerPack : BaseAppPack
{
    private static List<OpenApiInfo> _apiInfos;

    /// <summary>
    /// ctor
    /// </summary>
    public DefaultSwaggerPack()
    {
        Initialize();
    }

    /// <inheritdoc />
    public override int Order => 1;

    /// <inheritdoc />
    public override bool IsEnable
    {
        get
        {
            var setting = Configuration.GetSection(nameof(DocumentOptions)).Get<DocumentOptions>();
            return setting is { Enable: true };
        }
    }


    /// <summary>
    /// 初始化api info
    /// </summary>
    private static void Initialize()
    {
        //获取用户自定义分组的API
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
                    //version在应用ApiVersion库之后需要重新处理
                    Version = "v1",
                    Description = apiInfo.Description
                });
            }
        }
    }

    /// <inheritdoc />
    public override void ConfigureServices(IServiceCollection services)
    {
        var setting = Configuration.GetSection(nameof(DocumentOptions)).Get<DocumentOptions>();
        if (setting?.Enable != true)
        {
            return;
        }

        services.AddSwaggerGen(c =>
        {
            //没有分组的api,swagger使用以下name:v1为json文件的路由
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Version = "v1",
                Title = "Api Document",
                Description = "当前为默认无分组api",
            });

            //添加自定义分组
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

            //加载运行目录下xml注释文件
            Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.xml")
                .ToList()
                .ForEach(file => c.IncludeXmlComments(file, true));


            //************************************************* Bearer Token *****************************************************//

            c.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
            {
                Description = "请输入\"Bearer {Token}\" ",
                Name = HeaderNames.Authorization,
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = JwtBearerDefaults.AuthenticationScheme
                        }
                    },
                    Array.Empty<string>()
                }
            });


            c.OperationFilter<AddResponseHeadersFilter>();
            c.OperationFilter<AppendAuthorizeToSummaryOperationFilter>();
            c.OperationFilter<SecurityRequirementsOperationFilter>(JwtBearerDefaults.AuthenticationScheme);

            //************************************************* OAuth2.0 Token *****************************************************//

            //eg:配置文件appsettings.json的key如果存在":"，那么解析将会失败
            var needFixeScopes = setting.Scopes
                .Where(x => x.Key.Contains('|'))
                .ToDictionary(x => x.Key, x => x.Value);

            //移除｜相关key
            foreach (var scope in needFixeScopes)
                setting.Scopes.Remove(scope.Key);

            //修改正确的key，
            foreach ((string key, string value) in needFixeScopes)
            {
                setting.Scopes.TryAdd(key.Replace("|", ":"), value);
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
        });
    }

    /// <inheritdoc />
    public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        var setting = Configuration.GetSection(nameof(DocumentOptions)).Get<DocumentOptions>();
        if (setting?.Enable != true)
        {
            return;
        }

        var virtualPath = Configuration.GetValue("VirtualPath", string.Empty);

        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            //默认没分组
            c.SwaggerEndpoint($"{virtualPath}/swagger/v1/swagger.json", "Api Document");
            _apiInfos.ForEach(info => { c.SwaggerEndpoint($"{virtualPath}/swagger/{info.Title}/swagger.json", info.Title); });

            c.OAuthClientId(setting.ClientId);
            c.OAuthClientSecret(setting.ClientSecret);
            c.OAuthUsePkce();
        });
    }
}