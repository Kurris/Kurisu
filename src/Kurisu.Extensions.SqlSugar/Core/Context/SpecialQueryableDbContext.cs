using System.Linq.Expressions;
using Kurisu.AspNetCore.Abstractions.Authentication;
using Kurisu.AspNetCore.Abstractions.DataAccess;
using Kurisu.AspNetCore.Abstractions.DataAccess.Contract;
using Kurisu.AspNetCore.Abstractions.DataAccess.Contract.Field;
using Kurisu.AspNetCore.Abstractions.DataAccess.Core.Context;
using Kurisu.AspNetCore.Abstractions.State;
using Kurisu.AspNetCore.DataAccess.SqlSugar.Attributes;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;

namespace Kurisu.Extensions.SqlSugar.Core.Context;

internal abstract class SpecialQueryableDbContext : AbstractDbContext<ISqlSugarClient>, ISqlSugarDbContext
{
    private readonly IStateSnapshotManager<DbOperationState> _snapshotManager;

    protected SpecialQueryableDbContext(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _snapshotManager = ServiceProvider.GetRequiredService<IStateSnapshotManager<DbOperationState>>();
    }

    public virtual void CodeFirstInitTables(params Type[] tables)
    {
        Client.DbMaintenance.CreateDatabase();
        foreach (var table in tables)
        {
            Client.CodeFirst.InitTables(table);

            if (table.IsAssignableTo(typeof(IIndexConfigurator)))
            {
                var tableName = Client.EntityMaintenance.GetTableName(table);
                var handler = (IIndexConfigurator)Activator.CreateInstance(table)!;
                var indexModels = handler.GetIndexConfigs();
                foreach (var indexModel in indexModels)
                {
                    if (!Client.DbMaintenance.IsAnyIndex(indexModel.IndexName))
                    {
                        Client.DbMaintenance.CreateIndex(tableName, indexModel.ColumnNames, indexModel.IndexName, indexModel.IsUnique);
                    }
                }
            }
        }
    }

    public new virtual ISqlSugarClient Client => base.Client;

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
        if (!_snapshotManager.Current.GetEnableCrossTenant<T>())
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
        if (!_snapshotManager.Current.GetEnableDataPermission<T>())
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


    public override void IgnoreTenant(Action action)
    {
        using (_snapshotManager.BeginScope(s =>
               {
                   s.IgnoreTenant = true;
                   Client.QueryFilter.ClearAndBackup<ITenantId>();
               }))
        {
            try
            {
                action();
            }
            finally
            {
                Client.QueryFilter.Restore();
            }
        }
    }

    public override async Task IgnoreTenantAsync(Func<Task> func)
    {
        await using (_snapshotManager.BeginScopeAsync(s =>
                     {
                         s.IgnoreTenant = true;
                         Client.QueryFilter.ClearAndBackup<ITenantId>();
                     }))
        {
            try
            {
                await func();
            }
            finally
            {
                Client.QueryFilter.Restore();
            }
        }
    }

    public override void IgnoreSoftDeleted(Action todo)
    {
        using (_snapshotManager.BeginScope(s =>
               {
                   s.IgnoreSoftDeleted = true;
                   Client.QueryFilter.ClearAndBackup<ISoftDeleted>();
               }))
        {
            try
            {
                todo();
            }
            finally
            {
                Client.QueryFilter.Restore();
            }
        }
    }

    public override async Task IgnoreSoftDeletedAsync(Func<Task> todo)
    {
        await using (_snapshotManager.BeginScopeAsync(s =>
                     {
                         s.IgnoreSoftDeleted = true;
                         Client.QueryFilter.ClearAndBackup<ISoftDeleted>();
                     }))
        {
            try
            {
                await todo();
            }
            finally
            {
                Client.QueryFilter.Restore();
            }
        }
    }

    public override void EnableCrossTenant(Type[] ignoreTypes, Action todo)
    {
        using (_snapshotManager.BeginScope(s =>
               {
                   s.EnableCrossTenant = true;
                   s.CrossTenantIgnoreTypes = ignoreTypes ?? Array.Empty<Type>();
               }))
        {
            IgnoreTenant(todo);
        }
    }

    public override async Task EnableCrossTenantAsync(Type[] ignoreTypes, Func<Task> todo)
    {
        await using (_snapshotManager.BeginScopeAsync(s =>
                     {
                         s.EnableCrossTenant = true;
                         s.CrossTenantIgnoreTypes = ignoreTypes ?? Array.Empty<Type>();
                     }))
        {
            await IgnoreTenantAsync(todo);
        }
    }

    public override void EnableDataPermission(Type[] ignoreTypes, Action todo)
    {
        using (_snapshotManager.BeginScope(s =>
               {
                   s.EnableDataPermission = true;
                   s.DataPermissionIgnoreTypes = ignoreTypes ?? Array.Empty<Type>();
               }))
        {
            todo();
        }
    }

    public override async Task EnableDataPermissionAsync(Type[] ignoreTypes, Func<Task> todo)
    {
        await using (_snapshotManager.BeginScopeAsync(s =>
                     {
                         s.EnableDataPermission = true;
                         s.DataPermissionIgnoreTypes = ignoreTypes ?? Array.Empty<Type>();
                     }))
        {
            await todo();
        }
    }
}