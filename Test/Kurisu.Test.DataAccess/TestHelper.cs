using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
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
using SqlSugar;
using Kurisu.AspNetCore.DataAccess.SqlSugar.Options;

namespace Kurisu.Test.DataAccess;

[ExcludeFromCodeCoverage]
public class TestHelper
{
    /// <summary>
    /// 获取服务提供器并按环境变量选择数据库类型（TEST_DB_PROVIDER）和连接字符串（TEST_DB_CONNECTION）。
    /// 支持 mysql、sqlserver。默认不再使用 sqlite（如果未配置则抛出说明）。
    /// </summary>
    public static IServiceProvider GetServiceProvider()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .Build();
        services.AddConfiguration(configuration);

        // 从环境变量或配置读取测试数据库提供者与连接字符串
        var provider = Environment.GetEnvironmentVariable("TEST_DB_PROVIDER") ?? configuration["TestDatabase:Provider"];
        var conn = Environment.GetEnvironmentVariable("TEST_DB_CONNECTION") ?? configuration["TestDatabase:ConnectionString"];

        if (string.IsNullOrWhiteSpace(provider))
        {
            throw new InvalidOperationException("测试数据库提供者未配置。请设置环境变量 TEST_DB_PROVIDER 为 'mysql' 或 'sqlserver'，并设置 TEST_DB_CONNECTION 为相应的连接字符串，或在 TestDatabase 节点中配置。示例： TEST_DB_PROVIDER=mysql  TEST_DB_CONNECTION='server=127.0.0.1;port=3306;database=test;user=root;pwd=...;' ");
        }

        provider = provider.Trim().ToLowerInvariant();

        DbType dbType;
        switch (provider)
        {
            case "mysql":
            case "mysqlconnector":
                dbType = DbType.MySqlConnector;
                break;
            case "sqlserver":
            case "mssql":
                dbType = DbType.SqlServer;
                break;
            default:
                throw new InvalidOperationException($"不支持的 TEST_DB_PROVIDER: {provider}. 仅支持 'mysql' 或 'sqlserver'.");
        }

        if (string.IsNullOrWhiteSpace(conn))
        {
            // 尝试读取原有 SqlSugarOptions 配置
            var cfgConn = configuration.GetSection("SqlSugarOptions")?.Get<SqlSugarOptions>()?.DefaultConnectionString;
            if (!string.IsNullOrWhiteSpace(cfgConn))
            {
                conn = cfgConn;
            }
            else
            {
                throw new InvalidOperationException("测试数据库连接字符串未配置。请设置环境变量 TEST_DB_CONNECTION 或在配置文件中设置 TestDatabase:ConnectionString 或 SqlSugarOptions:DefaultConnectionString。");
            }
        }

        // 将选定的连接字符串注入到 SqlSugarOptions，以便 SqlSugar 的 IDbConnectionManager 使用
        services.Configure<SqlSugarOptions>(opt =>
        {
            opt.DefaultConnectionString = conn;
            // 保留其他默认项（如 Timeout 等），不做修改
        });

        // 注册一个 CurrentUser 用于 SqlSugar 的 AOP/过滤等
        services.AddSingleton<ICurrentUser>(sp =>
        {
            var jwtOptions = sp.GetService<IOptions<JwtOptions>>()!.Value;

            var token = JwtEncryption.GenerateToken(
                new List<Claim>()
                {
                    new("sub", 3.ToString()),
                    new("role", "admin"),
                    new("name", "ligy"),
                    new("userType", "normal"),
                    new("tenant", "1234"),
                    new("code", "DL001")
                },
                jwtOptions.SecretKey, jwtOptions.Issuer!, jwtOptions.Audience!, 3600);

            return GetResolver(token);
        });

        // 注册 SqlSugar（根据 provider）
        services.AddSqlSugar(dbType);

        var sp = services.BuildServiceProvider(true);
        return sp.CreateScope().ServiceProvider;
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
            claims.Add(JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.TryGetValue(claim.Type, out var value)
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