using Kurisu.Test.DataAccess.Entities;

namespace Kurisu.Test.DataAccess.Filter.Mock;

public interface ICrossTenantService
{
    Task<List<Test1Entity>> QueryWithCrossTenantAsync();
    Task InsertAsync(Test1Entity entity);
    Task<Test1Entity> InsertWithTenantParameterAsync(string tenantId, Test1Entity entity);
    Task<List<Test1Entity>> QueryWithTenantParameterAsync(string tenantId);
    Task<List<Test1Entity>> QueryWithTenantInputAsync(UseTenantInput input);
    Task<List<Test1Entity>> QueryWithResolverAsync(string tenantId);
    Task<List<Test1Entity>> QueryWithMissingTenantAsync(string tenantId);
    Task<List<string>> QueryNestedTenantAsync(string outerTenantId, string innerTenantId);
}

public class UseTenantInput
{
    public string TenantId { get; set; }
}
