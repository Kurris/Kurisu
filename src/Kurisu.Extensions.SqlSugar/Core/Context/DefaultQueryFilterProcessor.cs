using Kurisu.AspNetCore.Abstractions.DataAccess;
using Kurisu.AspNetCore.Abstractions.DataAccess.Contract.Field;
using Kurisu.Extensions.ContextAccessor.Abstractions;
using Kurisu.Extensions.SqlSugar.Context;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;

namespace Kurisu.Extensions.SqlSugar.Core.Context;

public class DefaultQueryFilterProcessor(IServiceProvider serviceProvider) : IQueryFilterProcessor
{
    private readonly IContextSnapshotManager<DbOperationState> _snapshotManager = serviceProvider.GetRequiredService<IContextSnapshotManager<DbOperationState>>();
    private const string TenantIdName = nameof(ITenantId.TenantId);

    public ISugarQueryable<T> Apply<T>(ISugarQueryable<T> query)
    {
        query = TryEnableCrossTenantFilter(query);
        query = TryEnableDataPermissionFilter(query);
        return query;
    }

    protected virtual ISugarQueryable<T> TryEnableCrossTenantFilter<T>(ISugarQueryable<T> query)
    {
        if (!IsCrossTenantEnabled())
            return query;

        //是否继承 ITenantId
        var type = typeof(T);
        if (!type.IsAssignableTo(typeof(ITenantId))) return query;

        // 空集合会被 SqlSugar 翻译为 1=2，用于拒绝没有 tenants claim 的跨租户查询，避免误查全量数据。
        var tenantIdValues = serviceProvider.GetRequiredService<IDbTenantAccessor>().GetAccessibleTenantIds().ToList();

        return query.Where(QueryFilterExpressionHelper.BuildContainsExpression<T, string>(TenantIdName, tenantIdValues));
    }

    protected virtual ISugarQueryable<T> TryEnableDataPermissionFilter<T>(ISugarQueryable<T> query)
    {
        if (!IsDataPermissionEnabled())
            return query;

        var getDataPermissions = serviceProvider.GetRequiredService<IGetDataPermissions>();
        var permissionData = getDataPermissions.GetData<T>() ?? [];
        if (permissionData.Count == 0)
            return query.Where(_ => false);

        foreach (var kv in permissionData)
        {
            query = ApplyDataPermissionFilter(query, kv.Key, kv.Value ?? []);
        }

        return query;
    }

    protected virtual bool IsCrossTenantEnabled()
    {
        return _snapshotManager.ContextAccessor.Current.EnableCrossTenant;
    }

    protected virtual bool IsDataPermissionEnabled()
    {
        return _snapshotManager.ContextAccessor.Current.EnableDataPermission;
    }

    protected virtual ISugarQueryable<T> ApplyDataPermissionFilter<T>(ISugarQueryable<T> query, string propertyName, IReadOnlyList<object> rawValues)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);

        var property = QueryFilterExpressionHelper.GetPermissionProperty(typeof(T), propertyName);
        var itemType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        var convertedValues = QueryFilterExpressionHelper.ConvertPermissionValues(itemType, rawValues);
        var expression = QueryFilterExpressionHelper.BuildContainsExpression<T>(propertyName, itemType, convertedValues);
        return query.Where(expression);
    }
}