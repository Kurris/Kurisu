namespace Kurisu.Extensions.SqlSugar.Sharding;

internal static class ShardingEntityHelper
{
    public static bool IsEnabled<T>()
    {
        return IsEnabled(typeof(T));
    }

    public static bool IsEnabled(Type entityType)
    {
        return entityType.IsDefined(typeof(EnableShardingAttribute), inherit: false);
    }
}
