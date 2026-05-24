using Kurisu.AspNetCore.Abstractions.DataAccess.Aop;
using Kurisu.AspNetCore.Abstractions.DataAccess.Core.Context;
using Kurisu.Extensions.SqlSugar.Utils;
using Kurisu.Test.DataAccess.Entities;

namespace Kurisu.Test.DataAccess.Filter.Mock;

public class DataPermissionService : IDataPermissionService
{
    private readonly IDbContext _dbContext;

    public DataPermissionService(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [EnableDataPermission]
    public async Task<List<TestDataPermissionEntity>> QueryWithDataPermissionAsync()
    {
        return await _dbContext.Queryable<TestDataPermissionEntity>().ToListAsync();
    }

    public async Task InsertAsync(TestDataPermissionEntity entity)
    {
        await _dbContext.InsertAsync(entity);
    }
}
