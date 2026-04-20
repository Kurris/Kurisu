using Kurisu.AspNetCore.Abstractions.Utils.Disposables;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.Abstractions.DataAccess.Core.Context;

public abstract class AbstractDbContext<TOperationClient> : WriteAbstractDbContext<TOperationClient>, IDbContext where TOperationClient : class
{
    /// <summary>
    /// 数据源管理器栈：每次 CreateDatasourceScope 推入新的 Transient 管理器实例，Dispose 时弹出并还原到上一层的数据源与事务上下文
    /// </summary>
    private readonly Stack<IDatasourceManager<TOperationClient>> _managerStack = new();

    /// <summary>
    /// 已命名数据源管理器缓存：同名数据源在整个 DbContext 生命周期内复用同一管理器实例，以保证连接与事务的一致性
    /// </summary>
    private readonly Dictionary<string, IDatasourceManager<TOperationClient>> _namedManagers = new();

    protected AbstractDbContext(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

    /// <summary>
    /// 当前活跃的数据源管理器（栈顶）
    /// </summary>
    protected IDatasourceManager<TOperationClient> GenericDatasourceManager => _managerStack.Peek();

    /// <summary>
    /// 当前活跃的数据源管理器
    /// </summary>
    public IDatasourceManager DatasourceManager => GenericDatasourceManager;

    /// <summary>
    /// 当前活跃数据源的操作客户端（始终取栈顶管理器）
    /// </summary>
    protected TOperationClient Client => GenericDatasourceManager.GetCurrentClient();


    public IServiceProvider ServiceProvider { get; }
    public abstract ICodeFirstMode CodeFirst { get; }

    public virtual IDisposable CreateDatasourceScope(string name)
    {
        var connectionStringManager = ServiceProvider.GetRequiredService<IDbConnectionStringManager>();

        // 栈非空且名称相同：同一数据源无需切换，连接与事务复用，返回空操作
        if (_managerStack.Count > 0 && name == connectionStringManager.Current)
        {
            return new ActionScope(() => { });
        }

        // 切换前，将当前活跃管理器与其名称关联（延迟注册，确保 A→B→A 时可复用 A 的管理器）
        if (_managerStack.TryPeek(out var currentManager))
        {
            _namedManagers.TryAdd(connectionStringManager.Current, currentManager);
        }

        // 切换连接字符串（将 name 推入连接名称栈）
        var connScope = connectionStringManager.CreateScope(name);

        // 复用已存在的同名管理器，或创建新的 Transient 管理器
        IDatasourceManager<TOperationClient> manager;
        if (_namedManagers.TryGetValue(name, out var existingManager))
        {
            manager = existingManager;
        }
        else
        {
            manager = ServiceProvider.GetRequiredService<IDatasourceManager<TOperationClient>>();
            _namedManagers[name] = manager;
        }

        _managerStack.Push(manager);
        return new ActionScope(() =>
        {
            _managerStack.Pop();
            connScope.Dispose();
        });
    }

    public abstract IDisposable CreateDatasourceScope();

    public abstract IDisposable IgnoreTenant();

    public abstract IDisposable IgnoreSoftDeleted();

    public abstract IDisposable EnableDataPermission();

    public abstract IDisposable EnableCrossTenant();

    public abstract IDisposable IgnoreSharding();
}