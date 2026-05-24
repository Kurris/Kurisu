using System.Diagnostics.CodeAnalysis;
using Kurisu.AspNetCore.Abstractions.DataAccess.Core.Context;
using Kurisu.AspNetCore.Abstractions.Startup;
using Kurisu.Extensions.SqlSugar.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Test.DataAccess;

[Trait("Db", "Ignore")]
public class TestIgnore
{
    private readonly IServiceProvider _sp;

    [ExcludeFromCodeCoverage]
    public TestIgnore()
    {
        _sp = TestHelper.GetServiceProvider();
    }

    [Fact(DisplayName = "忽略软删除: 开启后过滤数减1, 释放后恢复")]
    public void IgnoreSoftDeleted()
    {
        var scope = _sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                var filterCount = ctx.AsSqlSugarDbContext().GetClient().QueryFilter.GetFilterList.Count;
                var filterCount2 = 0;

                using (ctx.IgnoreSoftDeleted())
                {
                    filterCount2 = ctx.AsSqlSugarDbContext().GetClient().QueryFilter.GetFilterList.Count;
                }

                Assert.Equal(2, filterCount);
                Assert.Equal(1, filterCount2);
            }
        }

    }


    [Fact(DisplayName = "忽略租户: 开启后过滤数减1, 释放后恢复")]
    public void IgnoreTenant()
    {
        var scope = _sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                var filterCount = ctx.AsSqlSugarDbContext().GetClient().QueryFilter.GetFilterList.Count;
                var filterCount2 = 0;
                using (ctx.IgnoreTenant())
                {
                    filterCount2 = ctx.AsSqlSugarDbContext().GetClient().QueryFilter.GetFilterList.Count;
                }

                Assert.Equal(2, filterCount);
                Assert.Equal(1, filterCount2);
            }
        }
    }

    /// <summary>
    /// 验证 EnableCrossTenant 嵌套在 IgnoreTenant 内部时不会破坏外层 QueryFilter 备份。
    /// SqlSugar 的 ClearAndBackup/Restore 是单槽备份，嵌套调用会覆盖备份导致租户过滤器永久丢失。
    /// 修复后 EnableCrossTenant 在 IgnoreTenant 已启用时不再重复调用 ClearAndBackup。
    /// </summary>
    [Fact(DisplayName = "跨租户嵌套忽略租户: 释放外层后租户过滤器正确恢复, 备份未被内层覆盖")]
    public void EnableCrossTenant_NestedInIgnoreTenant_DoesNotCorruptFilterBackup()
    {
        var scope = _sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                var client = ctx.AsSqlSugarDbContext().GetClient();
                var initialFilterCount = client.QueryFilter.GetFilterList.Count;
                Assert.Equal(2, initialFilterCount);

                int filterCountDuringIgnoreTenant;
                int filterCountDuringCrossTenant;
                int filterCountAfterCrossTenantDisposed;
                int filterCountAfterIgnoreTenantDisposed;

                using (ctx.IgnoreTenant())
                {
                    filterCountDuringIgnoreTenant = client.QueryFilter.GetFilterList.Count;
                    Assert.Equal(1, filterCountDuringIgnoreTenant);

                    using (ctx.EnableCrossTenant())
                    {
                        filterCountDuringCrossTenant = client.QueryFilter.GetFilterList.Count;
                    }

                    // EnableCrossTenant 释放后，外层 IgnoreTenant 的备份不应被破坏
                    filterCountAfterCrossTenantDisposed = client.QueryFilter.GetFilterList.Count;
                    Assert.Equal(1, filterCountAfterCrossTenantDisposed);
                }

                // 外层 IgnoreTenant 释放后，租户过滤器必须被正确恢复
                filterCountAfterIgnoreTenantDisposed = client.QueryFilter.GetFilterList.Count;
                Assert.Equal(initialFilterCount, filterCountAfterIgnoreTenantDisposed);
            }
        }
    }
}
