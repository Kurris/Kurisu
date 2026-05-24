using Kurisu.AspNetCore.Abstractions.DataAccess.Aop;
using Kurisu.AspNetCore.Abstractions.DataAccess.Core.Context;
using Kurisu.Extensions.SqlSugar.Utils;
using Kurisu.Test.DataAccess.Entities;

namespace Kurisu.Test.DataAccess.Filter.Mock;

public class CrossTenantService : ICrossTenantService
{
    private readonly IDbContext _dbContext;

    public CrossTenantService(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [EnableCrossTenant]
    public async Task<List<Test1Entity>> QueryWithCrossTenantAsync()
    {
        return await _dbContext.Queryable<Test1Entity>().ToListAsync();
    }

    public async Task InsertAsync(Test1Entity entity)
    {
        await _dbContext.InsertAsync(entity);
    }
}
