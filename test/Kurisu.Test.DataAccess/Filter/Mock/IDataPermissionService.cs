using Kurisu.Test.DataAccess.Entities;

namespace Kurisu.Test.DataAccess.Filter.Mock;

public interface IDataPermissionService
{
    Task<List<TestDataPermissionEntity>> QueryWithDataPermissionAsync();
    Task InsertAsync(TestDataPermissionEntity entity);
}
