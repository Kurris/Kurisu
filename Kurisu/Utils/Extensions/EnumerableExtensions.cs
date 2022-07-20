using System.Collections.Generic;

namespace Kurisu.Utils.Extensions
{
    public static class EnumerableExtensions
    {
        public static string StringJoin<T>(this IEnumerable<T> enumerable, string separator)
        {
            return string.Join(separator, enumerable);
        }

        public static string StringJoin<T>(this IEnumerable<T> enumerable, char separator)
        {
            return string.Join(separator, enumerable);
        }
    }
}