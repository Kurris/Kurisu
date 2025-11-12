using Kurisu.AspNetCore.Abstractions.DataAccess.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.Abstractions.DataAccess.Core.Context;

public abstract class AbstractDbContext<TOperationClient> : WriteAbstractDbContext<TOperationClient>, IDbContext where TOperationClient : class
{
    protected AbstractDbContext(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        ServiceProvider = serviceProvider;
        DatasourceManager = serviceProvider.GetRequiredService<IDatasourceManager>();
    }

    protected TOperationClient Client => DatasourceManager.GetCurrentClient<TOperationClient>();
    protected IServiceProvider ServiceProvider { get; }
    protected IDatasourceManager DatasourceManager { get; }


    public abstract void IgnoreTenant(Action todo);
    public abstract Task IgnoreTenantAsync(Func<Task> todo);

    public abstract void IgnoreSoftDeleted(Action todo);
    public abstract Task IgnoreSoftDeletedAsync(Func<Task> todo);

    public abstract void EnableDataPermission(Type[] ignoreTypes, Action todo);
    public abstract Task EnableDataPermissionAsync(Type[] ignoreTypes, Func<Task> todo);

    public abstract void EnableCrossTenant(Type[] ignoreTypes, Action todo);
    public abstract Task EnableCrossTenantAsync(Type[] ignoreTypes, Func<Task> todo);
}