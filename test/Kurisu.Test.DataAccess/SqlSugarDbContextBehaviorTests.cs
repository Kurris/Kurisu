using Kurisu.AspNetCore.Abstractions.DataAccess.Core.Context;
using Kurisu.AspNetCore.Abstractions.Startup;
using Kurisu.Extensions.SqlSugar.Utils;
using Kurisu.Test.DataAccess.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Test.DataAccess;

public class SqlSugarDbContextBehaviorTests
{
    [Fact]
    public async Task EmptyWriteCollections_AreHandledAsNoOps()
    {
        var serviceProvider = TestHelper.GetServiceProvider();
        using var disposableServiceProvider = serviceProvider as IDisposable;
        using var scope = serviceProvider.CreateScope();
        using var lifecycle = scope.ServiceProvider.InitLifecycle();
        var dbContext = scope.ServiceProvider.GetRequiredService<IDbContext>();
        var entities = new List<Test1Entity>();

        Assert.True(dbContext.Insert(entities));
        Assert.True(await dbContext.InsertAsync(entities));
        Assert.Equal(0, dbContext.Update(entities));
        Assert.Equal(0, await dbContext.UpdateAsync(entities));
        Assert.Equal(0, dbContext.Delete(entities));
        Assert.Equal(0, await dbContext.DeleteAsync(entities));
    }

    [Fact]
    public void CrossTenantQuery_WithoutTenantClaims_UsesEmptyContainsFilter()
    {
        var serviceProvider = TestHelper.GetServiceProvider();
        using var disposableServiceProvider = serviceProvider as IDisposable;
        using var scope = serviceProvider.CreateScope();
        using var lifecycle = scope.ServiceProvider.InitLifecycle();
        var dbContext = scope.ServiceProvider.GetRequiredService<IDbContext>();

        using (dbContext.CreateDatasourceScope())
        using (dbContext.EnableCrossTenant())
        {
            var sql = dbContext.AsSqlSugarDbContext().Queryable<Test1Entity>().ToSql().Key;

            Assert.Contains("1=2", sql.Replace(" ", string.Empty));
        }
    }
}
