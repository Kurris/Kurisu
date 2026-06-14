using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using AspectCore.Extensions.DependencyInjection;
using Kurisu.AspNetCore.Abstractions.Authentication;
using Kurisu.AspNetCore.Abstractions.DataAccess.Core;
using Kurisu.AspNetCore.Abstractions.Startup;
using Kurisu.Extensions.SqlSugar;
using Kurisu.Extensions.SqlSugar.Context;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;
using Xunit;

namespace Kurisu.Test.Framework.DependencyInjection;

[Trait("sqlsugar", "options")]
public class TestSqlSugarOptions
{
    [Fact]
    public void ResolveConnectionRegistry_WithEmptyAdditionalConnectionStrings_DoesNotThrow()
    {
        const string json = """
                            {
                              "DbOptions": {
                                "DbType": "MySqlConnector",
                                "DefaultConnectionString": "server=127.0.0.1;port=3306;userid=root;password=123456;database=test;charset=utf8mb4;",
                                "AdditionalConnectionStrings": {},
                                "Timeout": 30,
                                "SlowSqlTime": 1,
                                "EnableSqlLog": false,
                                "Generate": false
                              }
                            }
                            """;

        var services = new ServiceCollection();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var configuration = new ConfigurationBuilder()
            .AddJsonStream(stream)
            .Build();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddConfiguration(configuration);
        services.AddDependencyInjection();
        services.AddSqlSugar(DbType.MySqlConnector);

        var provider = services.BuildDynamicProxyProvider();
        var registry = provider.GetRequiredService<IDbConnectionRegistry>();

        Assert.Equal(
            "server=127.0.0.1;port=3306;userid=root;password=123456;database=test;charset=utf8mb4;",
            registry.GetConnectionString("DefaultConnectionString"));
    }

    [Fact]
    public void AddSqlSugar_RegistersNullDatabaseContextAccessorsByDefault()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddSqlSugar(DbType.MySqlConnector);
        var provider = services.BuildServiceProvider();

        var auditAccessor = Assert.IsType<NullDbAuditAccessor>(provider.GetRequiredService<IDbAuditAccessor>());
        Assert.IsType<DefaultDbTenantAccessor>(provider.GetRequiredService<IDbTenantAccessor>());
        Assert.IsType<SystemDbClock>(provider.GetRequiredService<IDbClock>());
        Assert.Equal(-1, auditAccessor.GetUserId());
        Assert.Equal("system", auditAccessor.GetUserName());
    }

    [Fact]
    public void SqlSugarServiceBuilder_UseCustomAccessors_ReplacesDefaults()
    {
        var services = new ServiceCollection();

        services.AddSqlSugar(DbType.MySqlConnector)
            .UseAuditAccessor<CustomAuditAccessor>()
            .UseTenantAccessor<CustomTenantAccessor>()
            .UseClock<CustomDbClock>();
        var provider = services.BuildServiceProvider();

        Assert.IsType<CustomAuditAccessor>(provider.GetRequiredService<IDbAuditAccessor>());
        Assert.IsType<CustomTenantAccessor>(provider.GetRequiredService<IDbTenantAccessor>());
        Assert.IsType<CustomDbClock>(provider.GetRequiredService<IDbClock>());
    }

    [Fact]
    public void SqlSugarServiceBuilder_UseAccessorFactories_ReplacesDefaults()
    {
        var services = new ServiceCollection();

        services.AddSqlSugar(DbType.MySqlConnector)
            .UseAuditAccessor(_ => new CustomAuditAccessor())
            .UseTenantAccessor(_ => new CustomTenantAccessor())
            .UseClock(_ => new CustomDbClock());
        var provider = services.BuildServiceProvider();

        Assert.IsType<CustomAuditAccessor>(provider.GetRequiredService<IDbAuditAccessor>());
        Assert.IsType<CustomTenantAccessor>(provider.GetRequiredService<IDbTenantAccessor>());
        Assert.IsType<CustomDbClock>(provider.GetRequiredService<IDbClock>());
    }

    [Fact]
    public void SqlSugarServiceBuilder_UseCurrentUserContext_AdaptsCurrentUser()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ICurrentUser, FakeCurrentUser>();

        services.AddSqlSugar(DbType.MySqlConnector).UseCurrentUserContext();
        var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var auditAccessor = scope.ServiceProvider.GetRequiredService<IDbAuditAccessor>();
            var tenantAccessor = scope.ServiceProvider.GetRequiredService<IDbTenantAccessor>();

            Assert.Equal(3, auditAccessor.GetUserId());
            Assert.Equal("ligy", auditAccessor.GetUserName());
            Assert.Equal("1234", tenantAccessor.GetTenantId());
            Assert.Equal(["1001", "1002"], tenantAccessor.GetAccessibleTenantIds());
        }
    }

    private class CustomAuditAccessor : IDbAuditAccessor
    {
        public object GetUserId()
        {
            return 9;
        }

        public string GetUserName()
        {
            return "custom";
        }
    }

    private class CustomTenantAccessor : IDbTenantAccessor
    {
        public string GetTenantId()
        {
            return "custom-tenant";
        }

        public IReadOnlyList<string> GetAccessibleTenantIds()
        {
            return ["custom-tenant"];
        }
    }

    private class CustomDbClock : IDbClock
    {
        public DateTime Now => new(2026, 6, 4, 10, 30, 0);
    }

    private class FakeCurrentUser : ICurrentUser
    {
        public string TenantKey => "tenant";

        public int GetUserId()
        {
            return 3;
        }

        public T GetUserId<T>()
        {
            return (T)Convert.ChangeType(GetUserId(), typeof(T));
        }

        public string GetName(string userClaimType = "name")
        {
            return "ligy";
        }

        public string GetAccessToken()
        {
            return "token";
        }

        public List<string> GetRoles()
        {
            return ["admin"];
        }

        public string GetUserClaim(string claimType)
        {
            return claimType == "tenants" ? "1001, 1002" : null;
        }

        public string GetTenantId()
        {
            return "1234";
        }
    }
}
