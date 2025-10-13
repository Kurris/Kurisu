using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using Kurisu.AspNetCore.Abstractions.Authentication;
using Kurisu.AspNetCore.Authentication;
using Kurisu.AspNetCore.Authentication.Defaults;
using Kurisu.AspNetCore.Authentication.Options;
using Kurisu.Extensions.SqlSugar.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Kurisu.Test.Framework.DependencyInjection;

[ExcludeFromCodeCoverage]
public class TestHelper
{
    public static IServiceProvider GetServiceProvider()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();
        services.AddSingleton(typeof(IConfiguration), configuration);
        services.AddConfiguration(configuration);
        // services.AddSingleton<ICurrentUser>(sp =>
        // {
        //     var jwtOptions = sp.GetService<IOptions<JwtOptions>>()!.Value;
        //
        //     var token = JwtEncryption.GenerateToken(
        //         new List<Claim>()
        //         {
        //             new("sub", 3.ToString()),
        //             new("role", "admin"),
        //             new("name", "ligy"),
        //             new("userType", "normal"),
        //             new("tenant", "1234"),
        //             new("code", "DL001")
        //         },
        //         jwtOptions.SecretKey, jwtOptions.Issuer!, jwtOptions.Audience!, 3600);
        //
        //     return GetResolver(token);
        // });
        services.AddDependencyInjection();
        services.AddSqlSugar();

        var serviceProvider = services.BuildServiceProvider();

        var type = Assembly.Load("Kurisu.AspNetCore").GetTypes().First(x => x.Name.Equals("InternalApp"));
        var propertyInfo = type.GetProperty("RootServices", BindingFlags.Static | BindingFlags.NonPublic)!;
        propertyInfo.SetValue(null, serviceProvider);

        return serviceProvider;
    }

    /// <summary>
    /// 获取用户信息处理器
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public static ICurrentUser GetResolver(string token)
    {
        var jwtSecurityToken = new JwtSecurityToken(token);

        var claims = new HashSet<Claim>();

        foreach (var claim in jwtSecurityToken.Claims)
        {
            claims.Add(JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.TryGetValue(claim.Type, out string value)
                ? new Claim(value, claim.Value)
                : claim);
        }

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        var httpContext = new DefaultHttpContext();
        // var type = Assembly.Load("Microsoft.AspNetCore.Http").GetType("Microsoft.AspNetCore.Http.DefaultHttpRequest");
        // _ = Activator.CreateInstance(type!, httpContext);

        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = httpContext
        };

        httpContextAccessor.HttpContext.Request.Headers.Add("Authorization", "Bearer " + token);
        httpContextAccessor.HttpContext.User = principal;

        return new DefaultCurrentUser(httpContextAccessor);
    }
}