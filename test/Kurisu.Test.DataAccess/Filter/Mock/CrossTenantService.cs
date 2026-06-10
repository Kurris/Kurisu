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

    [UseTenant("tenantId")]
    public async Task<Test1Entity> InsertWithTenantParameterAsync(string tenantId, Test1Entity entity)
    {
        await _dbContext.InsertAsync(entity);
        return entity;
    }

    [UseTenant("tenantId")]
    public async Task<List<Test1Entity>> QueryWithTenantParameterAsync(string tenantId)
    {
        return await _dbContext.Queryable<Test1Entity>().OrderBy(x => x.Id).ToListAsync();
    }

    [UseTenant("input.TenantId")]
    public async Task<List<Test1Entity>> QueryWithTenantInputAsync(UseTenantInput input)
    {
        return await _dbContext.Queryable<Test1Entity>().OrderBy(x => x.Id).ToListAsync();
    }

    [UseTenant<MethodArgumentTenantResolver>]
    public async Task<List<Test1Entity>> QueryWithResolverAsync(string tenantId)
    {
        return await _dbContext.Queryable<Test1Entity>().OrderBy(x => x.Id).ToListAsync();
    }

    [UseTenant("tenantId")]
    public async Task<List<Test1Entity>> QueryWithMissingTenantAsync(string tenantId)
    {
        return await _dbContext.Queryable<Test1Entity>().ToListAsync();
    }

    [UseTenant("outerTenantId")]
    public async Task<List<string>> QueryNestedTenantAsync(string outerTenantId, string innerTenantId)
    {
        var outerBefore = await QueryNamesAsync();

        List<string> inner;
        using (_dbContext.UseTenant(innerTenantId))
        {
            inner = await QueryNamesAsync();
        }

        var outerAfter = await QueryNamesAsync();
        return [outerBefore.Single(), inner.Single(), outerAfter.Single()];
    }

    private async Task<List<string>> QueryNamesAsync()
    {
        return await _dbContext.Queryable<Test1Entity>().OrderBy(x => x.Id).Select(x => x.Name).ToListAsync();
    }
}
