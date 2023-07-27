using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Kurisu.Utils.Extensions;

/// <summary>
/// 表达式树扩展类
/// </summary>
public static class ExpressionExtensions
{
    /// <summary>
    /// 类型对应的属性get方法缓存
    /// </summary>
    private static readonly Dictionary<string, List<PropertyMethodExpression>> _propertyGetMethodCache = new();

    /// <summary>
    /// 类型对应的属性set方法缓存
    /// </summary>
    private static readonly Dictionary<string, List<PropertyMethodExpression>> _propertySetMethodCache = new();

    /// <summary>
    /// 获取对象值
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="obj"></param>
    /// <param name="propertyName"></param>
    /// <returns></returns>
    public static TValue GetExpressionPropertyValue<TValue>(this object obj, string propertyName)
    {
        return ((Func<object, TValue>)Build<TValue>(_propertyGetMethodCache, obj, propertyName, (type, propertityInfo) =>
        {
            var parameter = Expression.Parameter(typeof(object));
            var convertExpression = Expression.Convert(parameter, type);
            var propertyExpression = Expression.Property(convertExpression, propertityInfo);
            var lambda = Expression.Lambda<Func<object, TValue>>(
                propertityInfo.PropertyType == typeof(TValue)
                    ? propertyExpression
                    : Expression.Convert(propertyExpression, typeof(TValue))
                , parameter);

            return lambda.Compile();

        }))(obj);
    }

    /// <summary>
    /// 设置对象值
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="obj"></param>
    /// <param name="propertyName"></param>
    /// <param name="value"></param>
    public static void SetExpressionPropertyValue<TValue>(this object obj, string propertyName, TValue value)
    {
        ((Action<object, TValue>)Build<TValue>(_propertySetMethodCache, obj, propertyName, (type, propertityInfo) =>
        {
            var parameterExpression = Expression.Parameter(typeof(object));
            var valueParameter = Expression.Parameter(typeof(TValue));
            var convertExpression = Expression.Convert(parameterExpression, type);
            var infoExpression = Expression.Property(convertExpression, propertityInfo);
            var assign = Expression.Assign(infoExpression,
                propertityInfo.PropertyType == typeof(TValue)
                    ? valueParameter
                    : Expression.Convert(valueParameter, propertityInfo.PropertyType)
            );

            var lambda = Expression.Lambda<Action<object, TValue>>(assign, parameterExpression, valueParameter);
            return lambda.Compile();

        }))(obj, value);
    }


    private static Delegate Build<TValue>(Dictionary<string, List<PropertyMethodExpression>> cache
        , object obj
        , string propertyName
        , Func<Type, PropertyInfo, Delegate> buildFunction)
    {
        var type = obj.GetType();
        var fullName = type.Namespace + "." + type.Name;
        var propertityInfo = type.GetProperties().FirstOrDefault(x => x.Name == propertyName);

        if (!cache.TryGetValue(fullName, out var functions))
        {
            functions = new List<PropertyMethodExpression>
            {
                new PropertyMethodExpression()
                {
                    PropertyName = propertyName,
                    Function = buildFunction(type,propertityInfo)
                }
            };

            cache.Add(fullName, functions);
        }

        if (!functions.Any(x => x.PropertyName == propertyName))
        {
            functions.Add(new PropertyMethodExpression()
            {
                PropertyName = propertyName,
                Function = buildFunction(type, propertityInfo)
            });
        }

        return functions.First(x => x.PropertyName == propertyName).Function;
    }


    private class PropertyMethodExpression
    {
        public string PropertyName { get; set; }

        public Delegate Function { get; set; }
    }
}
