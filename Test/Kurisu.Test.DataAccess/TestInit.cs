using Kurisu.AspNetCore.DataAccess.SqlSugar.Services;
using Kurisu.Test.DataAccess.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Test.DataAccess;

[Trait("Db","Init")]
public class TestInit
{
    private readonly IDbContext _dbContext;

    public TestInit( )
    {
        _dbContext = TestHelper.GetServiceProvider().GetRequiredService<IDbContext>();
    }
    
    [Fact]
    public void Init()
    {
        _dbContext.Client.CodeFirst.InitTables<Test1Entity>();
        _dbContext.Client.CodeFirst.InitTables<Test1WithSoftDeleteEntity>();
    }
}