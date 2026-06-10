using Kurisu.AspNetCore.Abstractions.DataAccess;
using Kurisu.AspNetCore.Abstractions.DataAccess.Contract;
using Kurisu.AspNetCore.Abstractions.DataAccess.Contract.Field;
using Kurisu.AspNetCore.Abstractions.DataAccess.Core;
using Kurisu.AspNetCore.Abstractions.DataAccess.Core.Context;
using Kurisu.Extensions.ContextAccessor.Abstractions;
using Kurisu.Extensions.SqlSugar.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SqlSugar;

namespace Kurisu.Extensions.SqlSugar.Core.Context;

public abstract class SpecialQueryableDbContext : AbstractDbContext<ISqlSugarClient>, ISqlSugarDbContext
{
    private readonly IQueryFilterProcessor _queryFilterProcessor;
    private readonly IContextSnapshotManager<DbOperationState> _snapshotManager;

    protected SpecialQueryableDbContext(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _queryFilterProcessor = ServiceProvider.GetRequiredService<IQueryFilterProcessor>();
        _snapshotManager = ServiceProvider.GetRequiredService<IContextSnapshotManager<DbOperationState>>();
    }

    public ISqlSugarClient GetClient()
    {
        return Client;
    }

    public override ICodeFirstMode CodeFirst => new SqlsugarCodeFirstMode(Client);


    public virtual ISugarQueryable<T> Queryable<T>()
    {
        var query = Client.Queryable<T>();
        return _queryFilterProcessor.Apply(query);
    }

    public virtual IUpdateable<T> Updateable<T>() where T : class, IEntity, new()
    {
        return Client.Updateable<T>();
    }

    public virtual IDeleteable<T> Deleteable<T>() where T : class, IEntity, new()
    {
        return Client.Deleteable<T>();
    }


    public override IDisposable IgnoreTenant()
    {
        return _snapshotManager.CreateScope(s =>
               {
                   s.IgnoreTenant = true;
                   Client.QueryFilter.ClearAndBackup<ITenantId>();
               },
               Client.QueryFilter.Restore);
    }

    public override IDisposable UseTenant(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentNullException(nameof(tenantId));
        }

        return _snapshotManager.CreateScope(s =>
            {
                s.UseTenantId = tenantId;
                if (!s.IgnoreTenant)
                {
                    Client.QueryFilter.ClearAndBackup<ITenantId>();
                    Client.QueryFilter.AddTableFilter<ITenantId>(x => x.TenantId == tenantId);
                }
            },
            Client.QueryFilter.Restore);
    }

    public override IDisposable IgnoreSoftDeleted()
    {
        return _snapshotManager.CreateScope(s =>
                {
                    s.IgnoreSoftDeleted = true;
                    Client.QueryFilter.ClearAndBackup<ISoftDeleted>();
                }, Client.QueryFilter.Restore);
    }


    public override IDisposable EnableCrossTenant()
    {
        var snapshotScope = _snapshotManager.CreateScope(s =>
        {
            s.EnableCrossTenant = true;
        });

        // 如果当前已处于 IgnoreTenant 作用域内，不再重复调用 ClearAndBackup，避免覆盖外层备份
        if (_snapshotManager.ContextAccessor.Current.IgnoreTenant)
        {
            return snapshotScope;
        }

        var tenantScope = IgnoreTenant();

        return new CompositeDisposableAction(tenantScope, snapshotScope);
    }

    public override IDisposable EnableDataPermission()
    {
        return _snapshotManager.CreateScope(s =>
                {
                    s.EnableDataPermission = true;
                });
    }

    public override IDisposable IgnoreSharding()
    {
        return _snapshotManager.CreateScope(s =>
        {
            s.IgnoreSharding = true;
        });
    }

    public override IDisposable CreateDatasourceScope()
    {
        var dbOptions = ServiceProvider.GetRequiredService<IOptions<DbOptions>>().Value;
        return CreateDatasourceScope(nameof(dbOptions.DefaultConnectionString));
    }
}
