using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.Abstractions.DataAccess.Core.Context;

public abstract class AbstractDbContext<TOperationClient> : WriteAbstractDbContext<TOperationClient>, IDbContext where TOperationClient : class
{
    protected AbstractDbContext(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        ServiceProvider = serviceProvider;
        DatasourceManager = serviceProvider.GetRequiredService<IDatasourceManager<TOperationClient>>();
    }

    protected TOperationClient Client => DatasourceManager.GetCurrentClient();
    protected IDatasourceManager<TOperationClient> DatasourceManager { get; }


    public IServiceProvider ServiceProvider { get; }
    public abstract ICodeFirstMode CodeFirst { get; }


    public abstract IDisposable CreateDatasourceScope(string name);
    public abstract IDisposable CreateDatasourceScope();


    public abstract IDisposable IgnoreTenant();

    public abstract IDisposable IgnoreSoftDeleted();

    public abstract IDisposable EnableDataPermission();

    public abstract IDisposable EnableCrossTenant();

    public abstract IDisposable IgnoreSharding();
}