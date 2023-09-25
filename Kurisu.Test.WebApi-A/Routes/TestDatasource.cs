using Kurisu.EFSharding.Core.DependencyInjection;
using Kurisu.EFSharding.Core.Metadata.Builder;
using Kurisu.EFSharding.Core.VirtualRoutes;
using Kurisu.EFSharding.Core.VirtualRoutes.DatasourceRoute.Abstractions;
using Kurisu.EFSharding.Helpers;

namespace Kurisu.Test.WebApi_A.Routes;

public class TestDatasource : AbstractShardingOperatorVirtualDataSourceRoute<Entity.Test, string>
{
    public TestDatasource(IShardingProvider shardingProvider) : base(shardingProvider)
    {
    }

    public override string ShardingKeyToDataSourceName(object shardingKey)
    {
        return "ds" + ShardingCoreHelper.GetStringHashCode(shardingKey.ToString()) % 3;
    }

    public override List<string> GetAllDatasourceNames()
    {
        return new List<string>
        {
            "ds0", "ds1", "ds2"
        };
    }

    public override bool AddDatasourceName(string dataSourceName)
    {
        throw new NotImplementedException();
    }

    public override void Configure(IShardingMetadataBuilder<Entity.Test> builder)
    {
        builder.SetProperty(o => o.Name);
    }

    protected override Func<string, bool> GetRouteToFilter(string shardingKey, ShardingOperatorEnum shardingOperator)
    {
        var shardingKeyToDataSourceName = ShardingKeyToDataSourceName(shardingKey);
        switch (shardingOperator)
        {
            case ShardingOperatorEnum.Equal: return t => t.Equals(shardingKeyToDataSourceName);
            default: return t => true;
        }
    }
}