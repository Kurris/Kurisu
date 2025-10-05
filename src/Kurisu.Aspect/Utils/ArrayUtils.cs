namespace AspectCore.Utils;

internal static class ArrayUtils
{
    public static T[] Empty<T>()
    {
        return EmptyArray<T>.Value;
    }

    private static class EmptyArray<T>
    {
        public static readonly T[] Value = Array.Empty<T>();
    }
}