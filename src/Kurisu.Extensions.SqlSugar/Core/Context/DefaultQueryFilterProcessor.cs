using System.Collections;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using Kurisu.AspNetCore.Abstractions.Authentication;
using Kurisu.AspNetCore.Abstractions.DataAccess;
using Kurisu.AspNetCore.Abstractions.DataAccess.Contract.Field;
using Kurisu.Extensions.ContextAccessor.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;

namespace Kurisu.Extensions.SqlSugar.Core.Context;

public class DefaultQueryFilterProcessor : IQueryFilterProcessor
{
    private readonly IServiceProvider _serviceProvider;

    public DefaultQueryFilterProcessor(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public ISugarQueryable<T> Apply<T>(ISugarQueryable<T> query)
    {
        query = TryEnableCrossTenantFilter(query);
        query = TryEnableDataPermissionFilter(query);
        return query;
    }

    protected virtual ISugarQueryable<T> TryEnableCrossTenantFilter<T>(ISugarQueryable<T> query)
    {
        var snapshotManager = _serviceProvider.GetRequiredService<IContextSnapshotManager<DbOperationState>>();
        if (!snapshotManager.ContextAccessor.Current.EnableCrossTenant)
            return query;

        //是否继承 ITenantId
        var type = typeof(T);
        if (!type.IsAssignableTo(typeof(ITenantId))) return query;

        var tenantsStr = _serviceProvider.GetRequiredService<ICurrentUser>().GetUserClaim("tenants");
        if (string.IsNullOrEmpty(tenantsStr))
            tenantsStr = "";

        // 空集合会被 SqlSugar 翻译为 1=2，用于拒绝没有 tenants claim 的跨租户查询，避免误查全量数据。
        var tenantIdValues = tenantsStr.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
        var tenantIdName = nameof(ITenantId.TenantId);
        return query.Where(BuildContainsExpression<T, string>(tenantIdName, tenantIdValues));
    }

    protected virtual ISugarQueryable<T> TryEnableDataPermissionFilter<T>(ISugarQueryable<T> query)
    {
        var snapshotManager = _serviceProvider.GetRequiredService<IContextSnapshotManager<DbOperationState>>();
        if (!snapshotManager.ContextAccessor.Current.EnableDataPermission)
            return query;

        var getDataPermissions = _serviceProvider.GetRequiredService<IGetDataPermissions>();
        var permissionData = getDataPermissions.GetData<T>();
        foreach (var kv in permissionData)
        {
            query = ApplyDataPermissionFilter(query, kv.Key, kv.Value);
        }

        return query;
    }

    protected virtual ISugarQueryable<T> ApplyDataPermissionFilter<T>(ISugarQueryable<T> query, string propertyName, IReadOnlyList<object> rawValues)
    {
        var property = typeof(T).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        if (property == null)
            throw new InvalidOperationException($"实体 {typeof(T).Name} 不存在数据权限字段 {propertyName}");

        var itemType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        var convertedValues = ConvertPermissionValues(itemType, rawValues);
        var expression = BuildContainsExpression<T>(propertyName, itemType, convertedValues);
        return query.Where(expression);
    }

    protected virtual IList ConvertPermissionValues(Type targetType, IReadOnlyList<object> rawValues)
    {
        var listType = typeof(List<>).MakeGenericType(targetType);
        var values = (IList)Activator.CreateInstance(listType)!;

        if (rawValues.Count == 0)
            return values;

        var firstValue = rawValues[0];

        if (firstValue is string)
        {
            foreach (var text in rawValues.Cast<string>())
            {
                values.Add(ConvertPermissionValue(targetType, text));
            }

            return values;
        }

        if (targetType.IsInstanceOfType(firstValue))
        {
            foreach (var rawValue in rawValues)
            {
                values.Add(rawValue);
            }

            return values;
        }

        foreach (var rawValue in rawValues)
        {
            values.Add(ConvertPermissionValue(targetType, rawValue));
        }

        return values;
    }

    protected virtual object ConvertPermissionValue(Type targetType, object rawValue)
    {
        if (targetType.IsInstanceOfType(rawValue))
            return rawValue;

        if (targetType == typeof(string))
            return rawValue.ToString()!;

        if (targetType == typeof(Guid))
            return rawValue is Guid guid ? guid : Guid.Parse(rawValue.ToString()!);

        if (targetType.IsEnum)
            return rawValue is string enumText
                ? Enum.Parse(targetType, enumText, ignoreCase: true)
                : Enum.ToObject(targetType, rawValue);

        return Convert.ChangeType(rawValue, targetType, CultureInfo.InvariantCulture)!;
    }

    protected virtual Expression<Func<T, bool>> BuildContainsExpression<T>(string propertyName, Type itemType, IList values)
    {
        var method = GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(m => m.Name == nameof(BuildContainsExpression) && m.IsGenericMethodDefinition && m.GetParameters().Length == 2);

        var genericMethod = method.MakeGenericMethod(typeof(T), itemType);
        return (Expression<Func<T, bool>>)genericMethod.Invoke(this, new object[] { propertyName, values })!;
    }

    protected virtual Expression<Func<T, bool>> BuildContainsExpression<T, TItem>(string propertyName, List<TItem> values)
    {
        var parameterExpression = Expression.Parameter(typeof(T));
        var prop = Expression.Property(parameterExpression, propertyName);
        var contains = typeof(List<TItem>).GetMethod("Contains", new[] { typeof(TItem) })!;
        var constant = Expression.Constant(values, typeof(List<TItem>));
        var containsExp = Expression.Call(constant, contains, prop);
        return Expression.Lambda<Func<T, bool>>(containsExp, parameterExpression);
    }
}