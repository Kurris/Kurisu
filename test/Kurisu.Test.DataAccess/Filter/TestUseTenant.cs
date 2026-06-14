using Kurisu.AspNetCore.Abstractions.DataAccess.Core.Context;
using Kurisu.AspNetCore.Abstractions.Startup;
using Kurisu.Extensions.SqlSugar.Utils;
using Kurisu.Test.DataAccess.Entities;
using Kurisu.Test.DataAccess.Filter.Mock;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Test.DataAccess.Filter;

[Trait("Db", "UseTenant")]
public class TestUseTenant
{
    private static IServiceProvider GetServiceProvider(string tenantId = "default-tenant")
    {
        return TestHelper.GetServiceProvider(tenantId, configureServices: services =>
        {
            services.AddScoped<ICrossTenantService, CrossTenantService>();
        });
    }

    private static async Task PrepareAsync(IDbContext ctx, params Test1Entity[] rows)
    {
        ctx.CodeFirst.EnsureTableExists(typeof(Test1Entity));
        await ctx.AsSqlSugarDbContext().Deleteable<Test1Entity>().ExecuteCommandAsync();
        foreach (var row in rows)
        {
            await ctx.AsSqlSugarDbContext().GetClient().Insertable(row).ExecuteCommandIdentityIntoEntityAsync();
        }
    }

    [Fact(DisplayName = "UseTenant参数: 方法参数租户用于查询过滤")]
    public async Task UseTenant_Parameter_FiltersQuery()
    {
        using var scope = GetServiceProvider("current-tenant").CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                await PrepareAsync(ctx,
                    new Test1Entity { Name = "tenant-a-row", Type = "normal", Age = 1, TenantId = "tenant-a" },
                    new Test1Entity { Name = "tenant-b-row", Type = "normal", Age = 1, TenantId = "tenant-b" });

                var service = scope.ServiceProvider.GetRequiredService<ICrossTenantService>();
                var rows = await service.QueryWithTenantParameterAsync("tenant-b");

                Assert.Single(rows);
                Assert.Equal("tenant-b-row", rows[0].Name);
            }
        }
    }

    [Fact(DisplayName = "UseTenant参数: 方法参数租户用于插入自动赋值")]
    public async Task UseTenant_Parameter_FillsTenantOnInsert()
    {
        using var scope = GetServiceProvider("current-tenant").CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                await PrepareAsync(ctx);

                var service = scope.ServiceProvider.GetRequiredService<ICrossTenantService>();
                var entity = await service.InsertWithTenantParameterAsync("job-tenant", new Test1Entity
                {
                    Name = "inserted-by-job",
                    Type = "normal",
                    Age = 2
                });

                Assert.Equal("job-tenant", entity.TenantId);
                var rows = await service.QueryWithTenantParameterAsync("job-tenant");
                Assert.Single(rows);
            }
        }
    }

    [Fact(DisplayName = "UseTenant参数: 支持JSON路径语法从DTO属性解析租户")]
    public async Task UseTenant_JsonPath_FiltersQuery()
    {
        using var scope = GetServiceProvider("current-tenant").CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                await PrepareAsync(ctx,
                    new Test1Entity { Name = "dto-a", Type = "normal", Age = 1, TenantId = "tenant-a" },
                    new Test1Entity { Name = "dto-b", Type = "normal", Age = 1, TenantId = "tenant-b" });

                var service = scope.ServiceProvider.GetRequiredService<ICrossTenantService>();
                var rows = await service.QueryWithTenantInputAsync(new UseTenantInput { TenantId = "tenant-a" });

                Assert.Single(rows);
                Assert.Equal("dto-a", rows[0].Name);
            }
        }
    }

    [Fact(DisplayName = "UseTenant解析器: 可从被拦截方法参数解析租户")]
    public async Task UseTenant_Resolver_CanReadInterceptedMethodArguments()
    {
        using var scope = GetServiceProvider("current-tenant").CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                await PrepareAsync(ctx,
                    new Test1Entity { Name = "resolver-a", Type = "normal", Age = 1, TenantId = "tenant-a" },
                    new Test1Entity { Name = "resolver-b", Type = "normal", Age = 1, TenantId = "tenant-b" });

                var service = scope.ServiceProvider.GetRequiredService<ICrossTenantService>();
                var rows = await service.QueryWithResolverAsync("tenant-b");

                Assert.Single(rows);
                Assert.Equal("resolver-b", rows[0].Name);
            }
        }
    }

    [Fact(DisplayName = "UseTenant参数: 解析不到租户时抛异常")]
    public async Task UseTenant_MissingTenant_Throws()
    {
        using var scope = GetServiceProvider("current-tenant").CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                await PrepareAsync(ctx);

                var service = scope.ServiceProvider.GetRequiredService<ICrossTenantService>();
                await Assert.ThrowsAsync<InvalidOperationException>(() => service.QueryWithMissingTenantAsync(""));
            }
        }
    }

    [Fact(DisplayName = "UseTenant作用域: 内层租户释放后恢复外层租户")]
    public async Task UseTenant_NestedScope_RestoresOuterTenant()
    {
        using var scope = GetServiceProvider("current-tenant").CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                await PrepareAsync(ctx,
                    new Test1Entity { Name = "outer", Type = "normal", Age = 1, TenantId = "outer-tenant" },
                    new Test1Entity { Name = "inner", Type = "normal", Age = 1, TenantId = "inner-tenant" });

                var service = scope.ServiceProvider.GetRequiredService<ICrossTenantService>();
                var names = await service.QueryNestedTenantAsync("outer-tenant", "inner-tenant");

                Assert.Equal(["outer", "inner", "outer"], names);
            }
        }
    }
}
