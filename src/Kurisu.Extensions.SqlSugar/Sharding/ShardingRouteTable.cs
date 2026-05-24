using Kurisu.Extensions.SqlSugar.Attributes.DataAnnotations;

namespace Kurisu.Extensions.SqlSugar.Sharding;

/// <summary>
/// 路由表
/// </summary>
[Table("ShardingRouteTables", "分片路由表")]
public class ShardingRouteTable : SugarEntity , IIndexConfigurator
{
    /// <summary>
    /// 租户id
    /// </summary>
    [Column("租户id", false)]
    public string TenantId { get; set; }

    /// <summary>
    /// 租户id
    /// </summary>
    [Column("表后缀", false)]
    public string TableSuffix { get; set; }

    public List<IndexModel> GetIndexConfigs()
    {
        return
        [
            new IndexModel(true, "idx_sharding_route_tenant_id", [nameof(TenantId)])
        ];
    }
}
