namespace Kurisu.EFSharding.Sharding.PaginationConfigurations;


[Flags]
public enum PaginationMatchEnum
{
    /// <summary>
    /// 必须是当前对象的属性
    /// </summary>
    Owner = 1,
    /// <summary>
    /// 只要名称一样就可以了
    /// </summary>
    Named = 1 << 1,
    /// <summary>
    /// 仅第一个匹配就可以了
    /// </summary>
    PrimaryMatch = 1 << 2
}