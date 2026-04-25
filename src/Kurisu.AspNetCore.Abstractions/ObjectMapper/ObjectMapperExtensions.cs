using System;
using System.Linq.Expressions;
using System.Reflection;
using Mapster;

namespace Kurisu.AspNetCore.Abstractions.ObjectMapper;

/// <summary>
/// 对象映射扩展
/// </summary>
public static class ObjectMapperExtensions
{
    /// <summary>
    /// 把source的值映射到TDestination上(新建对象)
    /// </summary>
    /// <param name="source"></param>
    /// <typeparam name="TDestination"></typeparam>
    /// <returns></returns>
    public static TDestination Map<TDestination>(this object source)
    {
        return source.Adapt<TDestination>();
    }

    /// <summary>
    /// 把source的值映射到TDestination上(新建对象)，并允许对目标的指定属性进行自定义赋值
    /// 支持一次传入多个属性的表达式与对应的值工厂，值工厂接收 source 对象并返回要写入目标属性的值。
    /// 示例： source.Map((x=>x.PropA, s=>((Source)s).A+"-x"), (x=>x.PropB, s=>123))
    /// </summary>
    public static TDestination Map<TSource, TDestination>(this TSource source, params (Expression<Func<TDestination, object>> target, Func<TSource, object> valueFactory)[] mappings)
    {
        var dest = source.Adapt<TDestination>();
        if (mappings == null || mappings.Length == 0) return dest;

        foreach (var (target, valueFactory) in mappings)
        {
            if (target == null) continue;
            var member = GetMemberInfo(target);
            if (member is PropertyInfo pi && pi.CanWrite)
            {
                object value = valueFactory != null ? valueFactory(source) : null;

                if (value != null)
                {
                    if (!pi.PropertyType.IsInstanceOfType(value))
                    {
                        value = Convert.ChangeType(value, pi.PropertyType);
                    }
                }

                pi.SetValue(dest, value);
            }
        }

        return dest;
    }

    private static MemberInfo GetMemberInfo<TDestination>(Expression<Func<TDestination, object>> expression)
    {
        if (expression == null) throw new ArgumentNullException(nameof(expression));

        Expression body = expression.Body;
        if (body is UnaryExpression unary && unary.NodeType == ExpressionType.Convert)
        {
            body = unary.Operand;
        }

        if (body is MemberExpression memberExpr)
        {
            return memberExpr.Member;
        }

        throw new ArgumentException("Expression must be a member access", nameof(expression));
    }

    /// <summary>
    /// 把source的值映射到destination上(更新操作)
    /// </summary>
    /// <param name="source"></param>
    /// <param name="destination"></param>
    /// <typeparam name="TDestination"></typeparam>
    /// <typeparam name="TSource"></typeparam>
    /// <returns></returns>
    public static TDestination MapTo<TSource, TDestination>(this TSource source, TDestination destination)
    {
        return source.Adapt(destination);
    }


    /// <summary>
    /// 把source的值映射到destination上,其中ignoreProperties指定了要忽略的属性(更新操作)
    /// </summary>
    public static TDestination MapToIgnore<TSource, TDestination>(
        this TSource source, TDestination destination,
        params Expression<Func<TDestination, object>>[] ignoreProperties)
    {
        var config = TypeAdapterConfig.GlobalSettings.Clone();
        var typeConfig = config.ForType<TSource, TDestination>();

        foreach (var property in ignoreProperties)
        {
            typeConfig.Ignore(property);
        }

        return source.Adapt(destination, config);
    }


    /// <summary>
    /// 把source的映射到destination,其中忽略source中为null的属性(更新操作)
    /// </summary>
    /// <param name="source"></param>
    /// <param name="destination"></param>
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="TDestination"></typeparam>
    /// <returns></returns>
    public static TDestination MapToIgnoreNull<TSource, TDestination>(this TSource source, TDestination destination)
    {
        return source.Adapt(destination, TypeAdapterConfig.GlobalSettings.Clone()
            .ForType<TSource, TDestination>()
            .IgnoreNullValues(true).Config
        );
    }
}