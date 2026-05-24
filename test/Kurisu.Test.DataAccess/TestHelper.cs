using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AspectCore.Extensions.DependencyInjection;
using Kurisu.AspNetCore.Abstractions.Authentication;
using Kurisu.AspNetCore.Authentication;
using Kurisu.AspNetCore.Authentication.Defaults;
using Kurisu.AspNetCore.Authentication.Options;
using Kurisu.Extensions.SqlSugar;
using Kurisu.Test.DataAccess.Trans.Mock;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SqlSugar;

namespace Kurisu.Test.DataAccess;

[ExcludeFromCodeCoverage]
public class TestHelper
{
    public static IServiceProvider GetServiceProvider(string tenantId = "1234", bool enableSharding = false, Action<IServiceCollection> configureServices = null)
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        services.AddSingleton(typeof(IConfiguration), configuration);
        services.AddConfiguration(configuration);
        services.AddSingleton<ICurrentUser>(sp =>
        {
            var jwtOptions = sp.GetRequiredService<IOptions<JwtOptions>>().Value;
            var token = BuildToken(tenantId);
            return GetResolver(token);
        });

        services.AddLogging();
        services.AddDependencyInjection();
        var sqlSugarBuilder = services.AddSqlSugar(DbType.MySqlConnector);
        if (enableSharding)
        {
            sqlSugarBuilder.EnableSharding();
        }
        // register split services: inner and outer implementations located in Trans/mock
        services.AddScoped<ITransactionalInnerService, TransactionalInnerService>();
        services.AddScoped<ITransactionalOuterService, TransactionalOuterService>();
        services.AddScoped<IDatasourceScopeService, DatasourceScopeService>();

        configureServices?.Invoke(services);

        var serviceProvider = services.BuildDynamicProxyProvider();

        return serviceProvider;
    }

    /// <summary>
    /// 构建 JWT Token（默认 claims：sub, role, name, userType, tenant, code）
    /// </summary>
    /// <param name="tenantId">租户ID</param>
    /// <param name="tenantsClaim">跨租户声明值，null 表示不添加该 claim</param>
    public static string BuildToken(string tenantId, string tenantsClaim = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        var jwtOptions = configuration.GetSection("JwtOptions").Get<JwtOptions>();

        var claims = new List<Claim>
        {
            new("sub", 3.ToString()),
            new("role", "admin"),
            new("name", "ligy"),
            new("userType", "normal"),
            new("tenant", tenantId),
            new("code", "DL001")
        };

        if (tenantsClaim != null)
        {
            claims.Add(new Claim("tenants", tenantsClaim));
        }

        return JwtEncryption.GenerateToken(claims, jwtOptions!.SecretKey, jwtOptions.Issuer!, jwtOptions.Audience!, 3600);
    }

    /// <summary>
    /// 获取用户信息处理器
    /// </summary>
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
        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = httpContext
        };

        httpContextAccessor.HttpContext.Request.Headers.Add("Authorization", "Bearer " + token);
        httpContextAccessor.HttpContext.User = principal;

        return new DefaultCurrentUser(httpContextAccessor);
    }
}
