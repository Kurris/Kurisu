using System.Collections;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

namespace Kurisu.Extensions.SqlSugar.Core.Context;

internal static class QueryFilterExpressionHelper
{
    private static readonly ConcurrentDictionary<(Type EntityType, string PropertyName), PropertyInfo> PermissionPropertyCache = new();

    private static readonly MethodInfo BuildContainsExpressionMethod = typeof(QueryFilterExpressionHelper)
        .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
        .Single(x => x.Name == nameof(BuildContainsExpressionCore) && x.IsGenericMethodDefinition);

    private static class ContainsMethodCache<TItem>
    {
        internal static readonly MethodInfo Method = typeof(List<TItem>).GetMethod(
            nameof(List<TItem>.Contains),
            [typeof(TItem)])!;
    }

    internal static PropertyInfo GetPermissionProperty(Type entityType, string propertyName)
    {
        return PermissionPropertyCache.GetOrAdd(
            (entityType, propertyName),
            static key =>
            {
                var property = key.EntityType.GetProperty(
                    key.PropertyName,
                    BindingFlags.Public | BindingFlags.Instance);

                if (property == null)
                    throw new InvalidOperationException(
                        $"实体类型 '{key.EntityType.FullName}' 不存在公共数据权限属性 '{key.PropertyName}'.");

                if (!property.CanRead)
                    throw new InvalidOperationException(
                        $"实体类型 '{key.EntityType.FullName}' 的数据权限属性 '{key.PropertyName}' 不可读.");

                return property;
            });
    }

    internal static IList ConvertPermissionValues(Type targetType, IReadOnlyList<object> rawValues)
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

    internal static Expression<Func<T, bool>> BuildContainsExpression<T>(
        string propertyName,
        Type itemType,
        IList values)
    {
        var method = BuildContainsExpressionMethod.MakeGenericMethod(typeof(T), itemType);
        return (Expression<Func<T, bool>>)method.Invoke(null, [propertyName, values])!;
    }

    internal static Expression<Func<T, bool>> BuildContainsExpression<T, TItem>(
        string propertyName,
        List<TItem> values)
    {
        return BuildContainsExpressionCore<T, TItem>(propertyName, values);
    }

    private static object ConvertPermissionValue(Type targetType, object rawValue)
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

    private static Expression<Func<T, bool>> BuildContainsExpressionCore<T, TItem>(
        string propertyName,
        IList values)
    {
        var typedValues = (List<TItem>)values;
        var parameter = Expression.Parameter(typeof(T));
        var property = Expression.Property(parameter, propertyName);
        var constant = Expression.Constant(typedValues, typeof(List<TItem>));
        var nullableType = Nullable.GetUnderlyingType(property.Type);

        Expression valueExpression = property;
        Expression hasValueExpression = null;
        if (nullableType != null)
        {
            if (nullableType != typeof(TItem))
                throw new InvalidOperationException(
                    $"实体类型 '{typeof(T).FullName}' 的数据权限属性 '{propertyName}' 类型与权限值类型不匹配.");

            hasValueExpression = Expression.Property(property, nameof(Nullable<int>.HasValue));
            valueExpression = Expression.Property(property, nameof(Nullable<int>.Value));
        }
        else if (property.Type != typeof(TItem))
        {
            throw new InvalidOperationException(
                $"实体类型 '{typeof(T).FullName}' 的数据权限属性 '{propertyName}' 类型与权限值类型不匹配.");
        }

        var containsExpression = Expression.Call(
            constant,
            ContainsMethodCache<TItem>.Method,
            valueExpression);
        Expression body = hasValueExpression == null
            ? containsExpression
            : Expression.AndAlso(hasValueExpression, containsExpression);

        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }
}