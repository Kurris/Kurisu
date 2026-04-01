using System.Linq.Expressions;
using Kurisu.AspNetCore.Abstractions.Authentication;
using Kurisu.AspNetCore.Abstractions.DataAccess;
using Kurisu.AspNetCore.Abstractions.DataAccess.Contract;
using Kurisu.AspNetCore.Abstractions.DataAccess.Contract.Field;
using Kurisu.AspNetCore.Abstractions.DataAccess.Core;
using Kurisu.AspNetCore.Abstractions.DataAccess.Core.Context;
using Kurisu.AspNetCore.DataAccess.SqlSugar.Attributes;
using Kurisu.Extensions.ContextAccessor.Abstractions;
using Kurisu.Extensions.SqlSugar.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SqlSugar;

namespace Kurisu.Extensions.SqlSugar.Core.Context;

public abstract class SpecialQueryableDbContext : AbstractDbContext<ISqlSugarClient>, ISqlSugarDbContext
{
    private readonly IContextSnapshotManager<DbOperationState> _snapshotManager;

    protected SpecialQueryableDbContext(IServiceProvider serviceProvider) : base(serviceProvider)
    {
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
        query = TryEnableTenantFilter(query);
        query = TryEnableDataPermission(query);
        return query;
    }

    private ISugarQueryable<T> TryEnableTenantFilter<T>(ISugarQueryable<T> query)
    {
        var type = typeof(T);
        if (!_snapshotManager.ContextAccessor.Current.EnableCrossTenant)
            return query;

        // 获取租户Id属性名（接口优先，其次查特性）
        var tenantIdName = type.IsAssignableTo(typeof(ITenantId))
            ? nameof(ITenantId.TenantId)
            : type.GetProperties().FirstOrDefault(x => x.IsDefined(typeof(TenantIdAttribute), false))?.Name;

        if (string.IsNullOrEmpty(tenantIdName))
            return query;

        var tenantsStr = ServiceProvider.GetRequiredService<ICurrentUser>().GetUserClaim("tenants");
        if (string.IsNullOrEmpty(tenantsStr))
            return query;

        var tenantIds = tenantsStr.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
        if (tenantIds.Count == 0)
            return query;

        return query.Where(BuildContainsExpression<T, string>(tenantIdName, tenantIds));
    }

    private ISugarQueryable<T> TryEnableDataPermission<T>(ISugarQueryable<T> query)
    {
        if (!_snapshotManager.ContextAccessor.Current.EnableDataPermission)
            return query;

        var permissionData = ServiceProvider.GetRequiredService<IGetDataPermissions>().GetData<T>() ?? new Dictionary<string, List<Guid>>();
        foreach (var kv in permissionData)
        {
            if (kv.Value == null || kv.Value.Count == 0)
                continue;

            query = query.Where(BuildContainsExpression<T, Guid>(kv.Key, kv.Value));
        }

        return query;
    }

    private static Expression<Func<T, bool>> BuildContainsExpression<T, TItem>(string propertyName, List<TItem> values)
    {
        var parameterExpression = Expression.Parameter(typeof(T)); //x =>
        var prop = Expression.Property(parameterExpression, propertyName); // x=> x.TenantId
        var eq = typeof(List<TItem>).GetMethod("Contains", new[] { typeof(TItem) })!; //Contains
        var constant = Expression.Constant(values, typeof(List<TItem>)); // tenantIds
        var containsExp = Expression.Call(constant, eq, prop); //tenantIds.Contains(x.TenantId)

        //// x => tenantIds.Contains(x.TenantId)
        return Expression.Lambda<Func<T, bool>>(containsExp, parameterExpression);
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

    public override IDisposable CreateDatasourceScope(string name)
    {
        return DatasourceManager.CreateScope(name);
    }

    public override IDisposable CreateDatasourceScope()
    {
        var dbOptions = ServiceProvider.GetService<IOptions<DbOptions>>().Value;
        return CreateDatasourceScope(nameof(dbOptions.DefaultConnectionString));
    }
}