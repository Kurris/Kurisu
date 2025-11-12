using Kurisu.AspNetCore.Abstractions.DataAccess.Core.Context;
using Kurisu.Extensions.SqlSugar.Extensions;
using Kurisu.Test.DataAccess.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Test.DataAccess;

[Trait("Db", "Init")]
public class TestInit
{
    private readonly IDbContext _dbContext;

    public TestInit()
    {
        _dbContext = TestHelper.GetServiceProvider().GetRequiredService<IDbContext>();
    }

    [Fact]
    public void Init()
    {
        _dbContext.AsSqlSugarDbContext().CodeFirstInitTables(typeof(Test1Entity), typeof(Test1WithSoftDeleteEntity));
    }
}