using System.Diagnostics.CodeAnalysis;
using Kurisu.AspNetCore.Abstractions.DataAccess.Core.Context;
using Kurisu.AspNetCore.Abstractions.Startup;
using Kurisu.Extensions.SqlSugar.Utils;
using Kurisu.Test.DataAccess.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Test.DataAccess;

[Trait("Db", "Delete")]
public class TestSoftDelete
{
    private readonly IServiceProvider _sp;

    [ExcludeFromCodeCoverage]
    public TestSoftDelete()
    {
        _sp = TestHelper.GetServiceProvider();
    }

    [Fact(DisplayName = "软删除: 删除后查询不可见, 忽略软删除后可查询到")]
    public async Task SoftDelete()
    {
        var scope = _sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (ctx.CreateDatasourceScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<IDbContext>();
                await dbContext.AsSqlSugarDbContext().Deleteable<Test1WithSoftDeleteEntity>().ExecuteCommandAsync();

                var entity = new Test1WithSoftDeleteEntity
                {
                    Name = "ligy",
                    Type = "normal",
                    Age = 28,
                };
                await dbContext.InsertAsync(entity);
                Assert.True(entity.Id > 0);

                var data = await dbContext.Queryable<Test1WithSoftDeleteEntity>().ToListAsync();
                Assert.Single(data);

                await dbContext.DeleteAsync(data);
                data = await dbContext.Queryable<Test1WithSoftDeleteEntity>().ToListAsync();
                Assert.Empty(data);

                using (dbContext.IgnoreSoftDeleted())
                {
                    data = await dbContext.Queryable<Test1WithSoftDeleteEntity>().ToListAsync();
                    Assert.Single(data);
                }

                data = await dbContext.Queryable<Test1WithSoftDeleteEntity>().ToListAsync();
                Assert.Empty(data);
            }
        }
    }

    [Fact(DisplayName = "软删除: 只更新IsDeleted, 不覆盖实体的其他旧字段")]
    public async Task SoftDelete_UpdatesOnlyIsDeleted()
    {
        using var scope = _sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (dbContext.CreateDatasourceScope())
            {
                var client = dbContext.AsSqlSugarDbContext().GetClient();
                await client.Deleteable<Test1WithSoftDeleteEntity>().ExecuteCommandAsync();

                var staleEntity = new Test1WithSoftDeleteEntity
                {
                    Name = "stale-name",
                    Type = "normal",
                    Age = 28,
                };
                await dbContext.InsertAsync(staleEntity);

                await client.Updateable<Test1WithSoftDeleteEntity>()
                    .SetColumns(x => x.Name == "database-name")
                    .Where(x => x.Id == staleEntity.Id)
                    .ExecuteCommandAsync();

                await dbContext.DeleteAsync(staleEntity);

                using (dbContext.IgnoreSoftDeleted())
                {
                    var deleted = await dbContext.Queryable<Test1WithSoftDeleteEntity>()
                        .SingleAsync(x => x.Id == staleEntity.Id);
                    Assert.True(deleted.IsDeleted);
                    Assert.Equal("database-name", deleted.Name);
                }
            }
        }
    }


    [Fact(DisplayName = "真删除: 删除后查询不可见, 忽略软删除后仍不可见")]
    public async Task RealDelete()
    {
        var scope = _sp.CreateScope();
        using (scope.ServiceProvider.InitLifecycle())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<IDbContext>();
            using (dbContext.CreateDatasourceScope())
            {
                await dbContext.AsSqlSugarDbContext().Deleteable<Test1WithSoftDeleteEntity>().ExecuteCommandAsync();

                await dbContext.InsertAsync(new Test1WithSoftDeleteEntity
                {
                    Name = "ligy",
                    Type = "normal",
                    Age = 28,
                });

                var data = await dbContext.Queryable<Test1WithSoftDeleteEntity>().ToListAsync();
                Assert.Single(data);

                await dbContext.DeleteAsync(data, true);
                data = await dbContext.Queryable<Test1WithSoftDeleteEntity>().ToListAsync();
                Assert.Empty(data);

                using (dbContext.IgnoreSoftDeleted())
                {
                    data = await dbContext.Queryable<Test1WithSoftDeleteEntity>().ToListAsync();
                    Assert.Empty(data);
                }

                data = await dbContext.Queryable<Test1WithSoftDeleteEntity>().ToListAsync();
                Assert.Empty(data);
            }
        }
    }
}
