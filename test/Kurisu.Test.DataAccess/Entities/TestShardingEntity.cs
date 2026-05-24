using Kurisu.AspNetCore.Abstractions.DataAccess.Contract.Field;
using Kurisu.Extensions.SqlSugar;
using Kurisu.Extensions.SqlSugar.Sharding;
using SqlSugar;

namespace Kurisu.Test.DataAccess.Entities;

[SugarTable("test_sharding_entity")]
[EnableSharding]
public class TestShardingEntity : SugarEntity, ITenantId
{
    public string Name { get; set; }

    public int Age { get; set; }

    public string TenantId { get; set; }
}
