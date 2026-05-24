using Kurisu.Test.DataAccess.Entities;

namespace Kurisu.Test.DataAccess.Filter.Mock;

public interface ICrossTenantService
{
    Task<List<Test1Entity>> QueryWithCrossTenantAsync();
    Task InsertAsync(Test1Entity entity);
}
