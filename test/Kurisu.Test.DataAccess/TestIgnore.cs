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

    [Fact]
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


    [Fact]
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
}