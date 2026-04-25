using System.Diagnostics.CodeAnalysis;
using Kurisu.AspNetCore.Abstractions.DataAccess.Contract.Field;
using Kurisu.AspNetCore.Abstractions.DataAccess.Core.Context;
using Kurisu.AspNetCore.Abstractions.Startup;
using Kurisu.Extensions.SqlSugar.Utils;
using Kurisu.Test.DataAccess.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Test.DataAccess;

[Trait("db", "delete")]
public class TestSoftDelete
{
    private readonly IServiceProvider _sp;

    [ExcludeFromCodeCoverage]
    public TestSoftDelete()
    {
        _sp = TestHelper.GetServiceProvider();
    }

    [Fact]
    public async Task SoftDelete()
    {
        var sp = TestHelper.GetServiceProvider();
        var scope = sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                var _dbContext = scope.ServiceProvider.GetRequiredService<IDbContext>();
                await _dbContext.AsSqlSugarDbContext().Deleteable<Test1WithSoftDeleteEntity>().ExecuteCommandAsync();

                await _dbContext.InsertAsync(new Test1WithSoftDeleteEntity
                {
                    Name = "ligy",
                    Type = "normal",
                    Age = 28,
                });

                var data = await _dbContext.Queryable<Test1WithSoftDeleteEntity>().ToListAsync();
                Assert.Single(data);

                await _dbContext.DeleteAsync(data);
                data = await _dbContext.Queryable<Test1WithSoftDeleteEntity>().ToListAsync();
                Assert.Empty(data);

                using (_dbContext.IgnoreSoftDeleted())
                {
                    data = await _dbContext.Queryable<Test1WithSoftDeleteEntity>().ToListAsync();
                    Assert.Single(data);
                }

                data = await _dbContext.Queryable<Test1WithSoftDeleteEntity>().ToListAsync();
                Assert.Empty(data);
            }
        }
    }


    [Fact]
    public async Task RealDelete()
    {
        var sp = TestHelper.GetServiceProvider();
        var scope = sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var _dbContext = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (_dbContext.CreateDatasourceScope())
            {
                await _dbContext.AsSqlSugarDbContext().Deleteable<Test1WithSoftDeleteEntity>().ExecuteCommandAsync();

                await _dbContext.InsertAsync(new Test1WithSoftDeleteEntity
                {
                    Name = "ligy",
                    Type = "normal",
                    Age = 28,
                });

                var data = await _dbContext.Queryable<Test1WithSoftDeleteEntity>().ToListAsync();
                Assert.Single(data);

                await _dbContext.DeleteAsync(data, true);
                data = await _dbContext.Queryable<Test1WithSoftDeleteEntity>().ToListAsync();
                Assert.Empty(data);

                using (_dbContext.IgnoreSoftDeleted())
                {
                    data = await _dbContext.Queryable<Test1WithSoftDeleteEntity>().ToListAsync();
                    Assert.Empty(data);
                }

                data = await _dbContext.Queryable<Test1WithSoftDeleteEntity>().ToListAsync();
                Assert.Empty(data);
            }
        }
    }
}