namespace Kurisu.EFSharding.Extensions;

public static class LinqExtension
{
    //public static void ForEach<T>(this IEnumerable<T> source)

#if !EFCORE5 || NETSTANDARD2_1
    public static HashSet<TSource> ToHashSet<TSource>(
        this IEnumerable<TSource> source,
        IEqualityComparer<TSource> comparer = null)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));
        return new HashSet<TSource>(source, comparer);
    }
#endif
    /// <summary>
    /// 求集合的笛卡尔积
    /// </summary>
    public static IEnumerable<IEnumerable<T>> Cartesian<T>(this IEnumerable<IEnumerable<T>> sequences)
    {
        IEnumerable<IEnumerable<T>> tempProduct = new[] {Enumerable.Empty<T>()};
        return sequences.Aggregate(tempProduct,
            (accumulator, sequence) =>
                from accseq in accumulator
                from item in sequence
                select accseq.Concat(new[] {item})
        );
    }

    public static bool IsEmpty<T>(this IEnumerable<T> source)
    {
        return source == null || !source.Any();
    }

    public static bool IsNotEmpty<T>(this IEnumerable<T> source)
    {
        return !source.IsEmpty();
    }

    private static readonly HashSet<string> _enumerableContainsNamespace = new HashSet<string>()
    {
        "System.Linq", "System.Collections.Generic"
    };

    public static bool IsInEnumerable(this string thisValue)
    {
        return _enumerableContainsNamespace.Contains(thisValue);
    }
}