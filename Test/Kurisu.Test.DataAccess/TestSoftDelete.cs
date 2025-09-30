using System.Diagnostics.CodeAnalysis;
using Kurisu.AspNetCore.DataAccess.Entity;
using Kurisu.AspNetCore.DataAccess.SqlSugar.Services;
using Kurisu.Test.DataAccess.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Test.DataAccess;

[Trait("db", "delete")]
public class TestSoftDelete
{
    private readonly IDbContext _dbContext;

    [ExcludeFromCodeCoverage]
    public TestSoftDelete()
    {
        _dbContext = TestHelper.GetServiceProvider().GetRequiredService<IDbContext>();
    }

    [Fact]
    public async Task SoftDelete()
    {
        await _dbContext.Deleteable<Test1WithSoftDeleteEntity>().ExecuteCommandAsync();

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

        _dbContext.Client.QueryFilter.ClearAndBackup<ISoftDeleted>();
        data = await _dbContext.Queryable<Test1WithSoftDeleteEntity>().ToListAsync();
        Assert.Single(data);
        _dbContext.Client.QueryFilter.Restore();

        data = await _dbContext.Queryable<Test1WithSoftDeleteEntity>().ToListAsync();
        Assert.Empty(data);
    }


    [Fact]
    public async Task RealDelete()
    {
        await _dbContext.Deleteable<Test1WithSoftDeleteEntity>().ExecuteCommandAsync();

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

        _dbContext.Client.QueryFilter.ClearAndBackup<ISoftDeleted>();
        data = await _dbContext.Queryable<Test1WithSoftDeleteEntity>().ToListAsync();
        Assert.Empty(data);
        _dbContext.Client.QueryFilter.Restore();

        data = await _dbContext.Queryable<Test1WithSoftDeleteEntity>().ToListAsync();
        Assert.Empty(data);
    }
}