using Kurisu.EFSharding.Core.DependencyInjection;
using Kurisu.EFSharding.Core.Metadata.Builder;
using Kurisu.EFSharding.VirtualRoutes.Mods;

namespace Kurisu.Test.WebApi_A.Routes;

public class TestTableMod : BaseSimpleShardingModKeyStringVirtualTableRoute<Entity.Test>
{
    public TestTableMod(IShardingProvider shardingProvider) : base(shardingProvider, 2, 3)
    {
    }

    public override void Configure(IShardingMetadataBuilder<Entity.Test> builder)
    {
        builder.SetProperty(x => x.Id);
    }
}