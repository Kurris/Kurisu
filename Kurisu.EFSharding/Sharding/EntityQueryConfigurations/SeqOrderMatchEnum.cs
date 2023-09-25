namespace Kurisu.EFSharding.Sharding.EntityQueryConfigurations;

[Flags]
public enum SeqOrderMatchEnum
{
    /// <summary>
    /// 所属对象
    /// </summary>
    Owner=1,
    /// <summary>
    /// 所属排序名称一样
    /// </summary>
    Named=1<<1
}