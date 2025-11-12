namespace Kurisu.AspNetCore.Abstractions.DataAccess.Core.Context;

public interface IFilterOperator
{
    void IgnoreTenant(Action todo);
    Task IgnoreTenantAsync(Func<Task> todo);

    void IgnoreSoftDeleted(Action todo);
    Task IgnoreSoftDeletedAsync(Func<Task> todo);

    void EnableDataPermission(Type[] ignoreTypes, Action todo);
    Task EnableDataPermissionAsync(Type[] ignoreTypes, Func<Task> todo);

    void EnableCrossTenant(Type[] ignoreTypes, Action todo);
    Task EnableCrossTenantAsync(Type[] ignoreTypes, Func<Task> todo);
}