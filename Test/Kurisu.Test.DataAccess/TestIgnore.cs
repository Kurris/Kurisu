using System.Diagnostics.CodeAnalysis;
using Kurisu.AspNetCore.DataAccess.SqlSugar.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Test.DataAccess;

[Trait("Db", "Ignore")]
public class TestIgnore
{
    private readonly IDbContext _dbContext;

    [ExcludeFromCodeCoverage]
    public TestIgnore()
    {
        _dbContext = TestHelper.GetServiceProvider().GetRequiredService<IDbContext>();
    }


    [Fact]
    public void IgnoreSoftDeleted()
    {
        var filterCount = _dbContext.Client.QueryFilter.GetFilterList.Count;
        var filterCount2 = 0;
        _dbContext.IgnoreSoftDeleted(() => { filterCount2 = _dbContext.Client.QueryFilter.GetFilterList.Count; });

        Assert.Equal(2, filterCount);
        Assert.Equal(1, filterCount2);
    }


    [Fact]
    public void IgnoreTenant()
    {
        var filterCount = _dbContext.Client.QueryFilter.GetFilterList.Count;
        var filterCount2 = 0;
        _dbContext.IgnoreTenant(() => { filterCount2 = _dbContext.Client.QueryFilter.GetFilterList.Count; });

        Assert.Equal(2, filterCount);
        Assert.Equal(1, filterCount2);
    }
}