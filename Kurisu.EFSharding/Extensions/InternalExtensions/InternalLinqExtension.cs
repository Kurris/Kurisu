namespace Kurisu.EFSharding.Extensions.InternalExtensions;

internal static class InternalLinqExtension
{
    public static IEnumerable<TSource> OrderByAscDescIf<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector,bool asc,
        IComparer<TKey>? comparer)
    {
        return asc?source.OrderBy(keySelector, comparer): source.OrderByDescending(keySelector, comparer);
    }
    public static IOrderedEnumerable<TSource> ThenByIf<TSource, TKey>(this IOrderedEnumerable<TSource> source, Func<TSource, TKey> keySelector, bool condition,
        IComparer<TKey>? comparer)
    {
        return condition ? source.ThenBy(keySelector, comparer) : source;
    }
    public static IOrderedEnumerable<TSource> ThenByDescendingIf<TSource, TKey>(this IOrderedEnumerable<TSource> source, Func<TSource, TKey> keySelector, bool condition,
        IComparer<TKey>? comparer)
    {
        return condition ? source.ThenByDescending(keySelector, comparer) : source;
    }
}