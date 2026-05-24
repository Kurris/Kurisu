using Kurisu.AspNetCore.Abstractions.DataAccess;
using Kurisu.AspNetCore.Abstractions.DataAccess.Core.Context;
using Kurisu.AspNetCore.Abstractions.Startup;
using Kurisu.Extensions.SqlSugar.Utils;
using Kurisu.Test.DataAccess.Entities;
using Kurisu.Test.DataAccess.Filter.Mock;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Test.DataAccess.Filter;

[Trait("Db", "DataPermission")]
public class TestDataPermission
{
    private IServiceProvider GetServiceProvider(MockDataPermissionProvider permissionProvider)
    {
        return TestHelper.GetServiceProvider(configureServices: services =>
        {
            services.AddSingleton<IGetDataPermissions>(permissionProvider);
            services.AddScoped<IDataPermissionService, DataPermissionService>();
        });
    }

    private async Task PrepareTableAsync(IDbContext ctx)
    {
        ctx.CodeFirst.EnsureTableExists(typeof(TestDataPermissionEntity));
        await ctx.AsSqlSugarDbContext().Deleteable<TestDataPermissionEntity>().ExecuteCommandAsync();
    }

    [Fact(DisplayName = "数据权限字符串过滤: DepartmentId IN ('dept-a','dept-b'), 仅返回匹配数据")]
    public async Task DataPermission_StringFilter_ReturnsOnlyMatchingDepartments()
    {
        var permission = new MockDataPermissionProvider();
        permission.SetPermission<TestDataPermissionEntity>("DepartmentId",
            new List<object> { "dept-a", "dept-b" });

        var sp = GetServiceProvider(permission);
        using var scope = sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                await PrepareTableAsync(ctx);
                var svc = scope.ServiceProvider.GetRequiredService<IDataPermissionService>();

                await svc.InsertAsync(new TestDataPermissionEntity { Name = "n1", DepartmentId = "dept-a" });
                await svc.InsertAsync(new TestDataPermissionEntity { Name = "n2", DepartmentId = "dept-b" });
                await svc.InsertAsync(new TestDataPermissionEntity { Name = "n3", DepartmentId = "dept-c" });

                var result = await svc.QueryWithDataPermissionAsync();

                Assert.Equal(2, result.Count);
                Assert.All(result, x => Assert.Contains(x.DepartmentId, new[] { "dept-a", "dept-b" }));
            }
        }
    }

    [Fact(DisplayName = "数据权限Guid过滤: 按Guid类型字段过滤, ConvertPermissionValues正确处理Guid")]
    public async Task DataPermission_GuidFilter_ConvertsAndReturnsMatching()
    {
        var guidA = Guid.NewGuid();
        var guidB = Guid.NewGuid();
        var guidC = Guid.NewGuid();

        var permission = new MockDataPermissionProvider();
        permission.SetPermission<TestDataPermissionEntity>("DepartmentId",
            new List<object> { guidA, guidB });

        var sp = GetServiceProvider(permission);
        using var scope = sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                await PrepareTableAsync(ctx);
                var svc = scope.ServiceProvider.GetRequiredService<IDataPermissionService>();

                await svc.InsertAsync(new TestDataPermissionEntity { Name = "g1", DepartmentId = guidA.ToString() });
                await svc.InsertAsync(new TestDataPermissionEntity { Name = "g2", DepartmentId = guidB.ToString() });
                await svc.InsertAsync(new TestDataPermissionEntity { Name = "g3", DepartmentId = guidC.ToString() });

                var result = await svc.QueryWithDataPermissionAsync();

                Assert.Equal(2, result.Count);
            }
        }
    }

    [Fact(DisplayName = "数据权限空值列表: 权限数据返回空Dictionary时不过滤, 返回全量数据")]
    public async Task DataPermission_EmptyPermission_ReturnsAllData()
    {
        // GetData<T> 返回空字典 → foreach 不执行 → query 无额外过滤
        var permission = new MockDataPermissionProvider();

        var sp = GetServiceProvider(permission);
        using var scope = sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                await PrepareTableAsync(ctx);
                var svc = scope.ServiceProvider.GetRequiredService<IDataPermissionService>();

                await svc.InsertAsync(new TestDataPermissionEntity { Name = "all1", DepartmentId = "any" });
                await svc.InsertAsync(new TestDataPermissionEntity { Name = "all2", DepartmentId = "any" });

                var result = await svc.QueryWithDataPermissionAsync();

                Assert.Equal(2, result.Count);
            }
        }
    }

    [Fact(DisplayName = "数据权限EnableDataPermission scope: 手动开启后仅返回匹配数据, 释放后恢复全量")]
    public async Task DataPermission_ManualScope_FiltersAndRestores()
    {
        var permission = new MockDataPermissionProvider();
        permission.SetPermission<TestDataPermissionEntity>("DepartmentId",
            new List<object> { "dept-scope" });

        var sp = GetServiceProvider(permission);
        using var scope = sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                await PrepareTableAsync(ctx);

                await ctx.InsertAsync(new TestDataPermissionEntity { Name = "s1", DepartmentId = "dept-scope" });
                await ctx.InsertAsync(new TestDataPermissionEntity { Name = "s2", DepartmentId = "other" });

                List<TestDataPermissionEntity> filtered;
                using (ctx.EnableDataPermission())
                {
                    filtered = await ctx.Queryable<TestDataPermissionEntity>().ToListAsync();
                }

                var all = await ctx.Queryable<TestDataPermissionEntity>().ToListAsync();

                Assert.Single(filtered);
                Assert.Equal("s1", filtered[0].Name);
                Assert.Equal(2, all.Count);
            }
        }
    }

    [Fact(DisplayName = "数据权限不存在属性名: 实体缺少指定属性时抛出InvalidOperationException")]
    public async Task DataPermission_InvalidPropertyName_ThrowsInvalidOperationException()
    {
        var permission = new MockDataPermissionProvider();
        permission.SetPermission<TestDataPermissionEntity>("NonExistentField",
            new List<object> { "value" });

        var sp = GetServiceProvider(permission);
        using var scope = sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                await PrepareTableAsync(ctx);
                var svc = scope.ServiceProvider.GetRequiredService<IDataPermissionService>();

                await Assert.ThrowsAsync<InvalidOperationException>(
                    async () => await svc.QueryWithDataPermissionAsync());
            }
        }
    }
}
