using System.Linq.Expressions;
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