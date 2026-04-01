using Kurisu.Extensions.SqlSugar.Attributes.DataAnnotations;

namespace Kurisu.Extensions.SqlSugar.Sharding;

/// <summary>
/// 路由表
/// </summary>
[Table("ShardingRouteTables", "路由表")]
public class ShardingRouteTable : SugarEntity
{
    /// <summary>
    /// 租户id
    /// </summary>
    [Column("租户id", false)]
    public string TanantId { get; set; }

    /// <summary>
    /// 租户id
    /// </summary>
    [Column("表后缀", false)]
    public string TableSuffix { get; set; }
}
