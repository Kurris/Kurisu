using System.Collections;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using Kurisu.AspNetCore.Abstractions.DataAccess;
using Kurisu.AspNetCore.Abstractions.DataAccess.Contract.Field;
using Kurisu.Extensions.ContextAccessor.Abstractions;
using Kurisu.Extensions.SqlSugar.Context;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;

namespace Kurisu.Extensions.SqlSugar.Core.Context;

public class DefaultQueryFilterProcessor(IServiceProvider serviceProvider) : IQueryFilterProcessor
{
    private static readonly ConcurrentDictionary<(Type EntityType, string PropertyName), PropertyInfo> PermissionPropertyCache = new();
    private static readonly ConcurrentDictionary<Type, MethodInfo> ContainsExpressionMethodCache = new();
    private static class ContainsMethodCache<TItem>
    {
        internal static readonly MethodInfo Method = typeof(List<TItem>).GetMethod(
            nameof(List<TItem>.Contains),
            [typeof(TItem)])!;
    }

    public ISugarQueryable<T> Apply<T>(ISugarQueryable<T> query)
    {
        query = TryEnableCrossTenantFilter(query);
        query = TryEnableDataPermissionFilter(query);
        return query;
    }

    protected virtual ISugarQueryable<T> TryEnableCrossTenantFilter<T>(ISugarQueryable<T> query)
    {
        var snapshotManager = serviceProvider.GetRequiredService<IContextSnapshotManager<DbOperationState>>();
        if (!snapshotManager.ContextAccessor.Current.EnableCrossTenant)
            return query;

        //是否继承 ITenantId
        var type = typeof(T);
        if (!type.IsAssignableTo(typeof(ITenantId))) return query;

        // 空集合会被 SqlSugar 翻译为 1=2，用于拒绝没有 tenants claim 的跨租户查询，避免误查全量数据。
        var tenantIdValues = serviceProvider.GetRequiredService<IDbTenantAccessor>().GetAccessibleTenantIds().ToList();
        var tenantIdName = nameof(ITenantId.TenantId);
        return query.Where(BuildContainsExpression<T, string>(tenantIdName, tenantIdValues));
    }

    protected virtual ISugarQueryable<T> TryEnableDataPermissionFilter<T>(ISugarQueryable<T> query)
    {
        var snapshotManager = serviceProvider.GetRequiredService<IContextSnapshotManager<DbOperationState>>();
        if (!snapshotManager.ContextAccessor.Current.EnableDataPermission)
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

    protected virtual ISugarQueryable<T> ApplyDataPermissionFilter<T>(ISugarQueryable<T> query, string propertyName, IReadOnlyList<object> rawValues)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);

        var property = PermissionPropertyCache.GetOrAdd(
            (typeof(T), propertyName),
            static key =>
            {
                var propertyInfo = key.EntityType.GetProperty(
                    key.PropertyName,
                    BindingFlags.Public | BindingFlags.Instance);

                if (propertyInfo == null)
                    throw new InvalidOperationException(
                        $"实体类型 '{key.EntityType.FullName}' 不存在公共数据权限属性 '{key.PropertyName}'.");

                if (!propertyInfo.CanRead)
                    throw new InvalidOperationException(
                        $"实体类型 '{key.EntityType.FullName}' 的数据权限属性 '{key.PropertyName}' 不可读.");

                return propertyInfo;
            });

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
        var method = ContainsExpressionMethodCache.GetOrAdd(
            GetType(),
            static processorType => processorType
                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                .Single(m => m.Name == nameof(BuildContainsExpression) &&
                             m.IsGenericMethodDefinition &&
                             m.GetParameters().Length == 2));

        var genericMethod = method.MakeGenericMethod(typeof(T), itemType);
        return (Expression<Func<T, bool>>)genericMethod.Invoke(this, new object[] { propertyName, values })!;
    }

    protected virtual Expression<Func<T, bool>> BuildContainsExpression<T, TItem>(string propertyName, List<TItem> values)
    {
        var parameterExpression = Expression.Parameter(typeof(T));
        var prop = Expression.Property(parameterExpression, propertyName);
        var contains = ContainsMethodCache<TItem>.Method;
        var constant = Expression.Constant(values, typeof(List<TItem>));
        var nullableType = Nullable.GetUnderlyingType(prop.Type);

        Expression valueExpression = prop;
        Expression hasValueExpression = null;
        if (nullableType != null)
        {
            if (nullableType != typeof(TItem))
                throw new InvalidOperationException(
                    $"实体类型 '{typeof(T).FullName}' 的数据权限属性 '{propertyName}' 类型与权限值类型不匹配.");

            hasValueExpression = Expression.Property(prop, nameof(Nullable<int>.HasValue));
            valueExpression = Expression.Property(prop, nameof(Nullable<int>.Value));
        }
        else if (prop.Type != typeof(TItem))
        {
            throw new InvalidOperationException(
                $"实体类型 '{typeof(T).FullName}' 的数据权限属性 '{propertyName}' 类型与权限值类型不匹配.");
        }

        var containsExp = Expression.Call(constant, contains, valueExpression);
        Expression body = hasValueExpression == null
            ? containsExp
            : Expression.AndAlso(hasValueExpression, containsExp);

        return Expression.Lambda<Func<T, bool>>(body, parameterExpression);
    }
}
