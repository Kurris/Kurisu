using Kurisu.AspNetCore.Abstractions.DataAccess.Core.Context;
using Kurisu.AspNetCore.Abstractions.Startup;
using Kurisu.Test.DataAccess.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Test.DataAccess;

[Trait("Db", "Init")]
public class TestInit
{
    [Fact]
    public void Init()
    {
        var sp = TestHelper.GetServiceProvider();
        var scope = sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                ctx.CodeFirst.EnsureTablesExists(typeof(Test1Entity), typeof(Test1WithSoftDeleteEntity));
            }
        }
    }
}