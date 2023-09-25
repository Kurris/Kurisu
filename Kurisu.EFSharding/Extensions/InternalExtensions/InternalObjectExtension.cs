namespace Kurisu.EFSharding.Extensions.InternalExtensions;

internal static class InternalObjectExtension
{
    public static T As<T>(this object obj) where T : class
    {
        return (T)obj;
    }
}