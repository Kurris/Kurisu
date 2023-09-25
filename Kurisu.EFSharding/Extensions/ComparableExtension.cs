namespace Kurisu.EFSharding.Extensions;

public static class ComparableExtension
{
    public static int SafeCompareToWith(this IComparable value, IComparable other, bool asc)
    {
        if (asc)
            return SafeCompareTo(value, other);
        return SafeCompareTo(other, value);
    }
    public static int SafeCompareTo(IComparable value, IComparable other)
    {
        if (null == value && null == other) {
            return 0;
        }
        if (null == value)
        {
            return -1;
        }
        if (null == other) {
            return 1;
        }
        return value.CompareTo(other);
    }
}