using Kurisu.AspNetCore.DataAccess.SqlSugar.Services;
using Kurisu.Test.DataAccess.Entities;

namespace Kurisu.Test.DataAccess;

[Trait("Db","Init")]
public class TestInit
{
    private readonly IDbContext _dbContext;

    public TestInit(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    [Fact]
    public void Init()
    {
        _dbContext.Client.CodeFirst.InitTables<Test1Entity>();
        _dbContext.Client.CodeFirst.InitTables<Test1WithSoftDeleteEntity>();
    }
}