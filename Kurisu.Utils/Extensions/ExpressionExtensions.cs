using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace Kurisu.Utils.Extensions;

/// <summary>
/// 表达式树扩展类
/// </summary>
public static class ExpressionExtensions
{
    public static object GetExpressionPropertyValue<T>(this T obj, PropertyInfo propertyInfo)
    {
        return obj.GetExpressionPropertyValue(propertyInfo.Name);
    }

    public static object GetExpressionPropertyValue<T>(this T obj, string name)
    {
        return ExpressionCache<T>.PropertyGetMethodCache.GetOrAdd(name, x =>
        {
            var parameter = Expression.Parameter(typeof(T));
            //eg:obj.Name
            var propertyExpression = Expression.Property(parameter, typeof(T).GetProperty(name)!);
            var lambda = Expression.Lambda<Func<T, object>>(Expression.Convert(propertyExpression, typeof(object)), parameter);
            return lambda.Compile();
        })(obj);
    }

    public static void SetExpressionPropertyValue<T>(this T obj, PropertyInfo propertyInfo, object value)
    {
        obj.SetExpressionPropertyValue(propertyInfo.Name, value);
    }

    public static void SetExpressionPropertyValue<T>(this T obj, string name, object value)
    {
        ExpressionCache<T>.PropertySetMethodCache.GetOrAdd(name, x =>
        {
            var propertyInfo = typeof(T).GetProperty(name)!;
            var parameterExpression = Expression.Parameter(typeof(T));
            var valueParameter = Expression.Parameter(typeof(object));
            var infoExpression = Expression.Property(parameterExpression, propertyInfo);
            var assign = Expression.Assign(infoExpression,
                Expression.Convert(valueParameter, propertyInfo.PropertyType)
            );

            var lambda = Expression.Lambda<Action<T, object>>(assign, parameterExpression, valueParameter);
            return lambda.Compile();
        })(obj, value);
    }

    public static object GetExpressionPropertyValueWithGetMethod<T>(this T obj, string name, out Func<T, object> method)
    {
        var v = obj.GetExpressionPropertyValue(name);
        method = ExpressionCache<T>.PropertyGetMethodCache[name];
        return v;
    }

    public static void SetExpressionPropertyValueWithGetMethod<T>(this T obj, string name, object value, out Action<T, object> method)
    {
        obj.SetExpressionPropertyValue(name, value);
        method = ExpressionCache<T>.PropertySetMethodCache[name];
    }


    private static class ExpressionCache<T>
    {
        static ExpressionCache()
        {
            PropertyGetMethodCache = new ConcurrentDictionary<string, Func<T, object>>();
            PropertySetMethodCache = new ConcurrentDictionary<string, Action<T, object>>();
        }

        /// <summary>
        /// 类型对应的属性get方法缓存
        /// </summary>
        public static ConcurrentDictionary<string, Func<T, object>> PropertyGetMethodCache { get; }

        /// <summary>
        /// 类型对应的属性set方法缓存
        /// </summary>
        public static ConcurrentDictionary<string, Action<T, object>> PropertySetMethodCache { get; }
    }
}