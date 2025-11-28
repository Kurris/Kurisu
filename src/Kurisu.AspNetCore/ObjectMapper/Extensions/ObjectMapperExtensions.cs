using Mapster;

namespace Kurisu.AspNetCore.ObjectMapper.Extensions;

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
    public static TDestination Map<TSource, TDestination>(this TSource source, TDestination destination)
    {
        return source.Adapt(destination);
    }

    /// <summary>
    /// 把source的值映射到TDestination上(新建对象)，忽略null值
    /// </summary>
    /// <param name="source"></param>
    /// <param name="destination"></param>
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="TDestination"></typeparam>
    /// <returns></returns>
    public static TDestination MapIgnoreNull<TSource, TDestination>(this TSource source, TDestination destination)
    {
        return source.Adapt(destination, TypeAdapterConfig.GlobalSettings.Clone()
            .ForType<TSource, TDestination>()
            .IgnoreNullValues(true).Config
        );
    }
}