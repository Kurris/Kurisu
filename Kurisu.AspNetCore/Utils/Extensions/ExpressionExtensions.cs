﻿using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace Kurisu.AspNetCore.Utils.Extensions;

/// <summary>
/// 表达式扩展类
/// </summary>
public static class ExpressionExtensions
{
    /// <summary>
    /// 获取属性值
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="predicate"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static object GetPropertyValue<T>(this T obj, Expression<Func<T, object>> predicate) => obj.GetPropertyValue((predicate.Body as MemberExpression)?.Member.Name);

    /// <summary>
    /// 获取属性值
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="propertyInfo"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static object GetPropertyValue<T>(this T obj, PropertyInfo propertyInfo) => obj.GetPropertyValue(propertyInfo.Name);

    /// <summary>
    /// 获取属性值
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="name"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static object GetPropertyValue<T>(this T obj, string name)
    {
        var handler = ExpressionCache<T>.PropertyGetMethodCache.GetOrAdd(name, x =>
        {
            var parameter = Expression.Parameter(typeof(T));
            //eg:obj.Name
            var propertyExpression = Expression.Property(parameter, typeof(T).GetProperty(x)!);
            var lambda = Expression.Lambda<Func<T, object>>(Expression.Convert(propertyExpression, typeof(object)), parameter);

            return lambda.Compile();
        });

        return handler(obj);
    }

    /// <summary>
    /// 设置属性值
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="predicate"></param>
    /// <param name="value"></param>
    /// <typeparam name="T"></typeparam>
    public static void SetPropertyValue<T>(this T obj, Expression<Func<T, object>> predicate, object value) => obj.SetPropertyValue((predicate.Body as MemberExpression)?.Member.Name, value);

    /// <summary>
    /// 设置属性值
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="propertyInfo"></param>
    /// <param name="value"></param>
    /// <typeparam name="T"></typeparam>
    public static void SetPropertyValue<T>(this T obj, PropertyInfo propertyInfo, object value) => obj.SetPropertyValue(propertyInfo.Name, value);

    /// <summary>
    /// 设置属性值
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="name"></param>
    /// <param name="value"></param>
    /// <typeparam name="T"></typeparam>
    public static void SetPropertyValue<T>(this T obj, string name, object value)
    {
        var handler = ExpressionCache<T>.PropertySetMethodCache.GetOrAdd(name, x =>
        {
            var propertyInfo = typeof(T).GetProperty(x)!;
            var parameterExpression = Expression.Parameter(typeof(T));
            var valueParameter = Expression.Parameter(typeof(object));
            var infoExpression = Expression.Property(parameterExpression, propertyInfo);
            var assign = Expression.Assign(infoExpression,
                Expression.Convert(valueParameter, propertyInfo.PropertyType)
            );

            var lambda = Expression.Lambda<Action<T, object>>(assign, parameterExpression, valueParameter);
            return lambda.Compile();
        });

        handler(obj, value);
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