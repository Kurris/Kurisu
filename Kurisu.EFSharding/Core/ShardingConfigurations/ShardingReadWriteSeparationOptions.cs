using Kurisu.EFSharding.Core.DependencyInjection;
using Kurisu.EFSharding.Sharding.ReadWriteConfigurations;

namespace Kurisu.EFSharding.Core.ShardingConfigurations;

public class ShardingReadWriteSeparationOptions
{
    public Func<IShardingProvider, IDictionary<string, IEnumerable<string>>> ReadWriteSeparationConfigure
    {
        get;
        set;
    }

    public Func<IShardingProvider, IDictionary<string, IEnumerable<ReadNode>>> ReadWriteNodeSeparationConfigure
    {
        get;
        set;
    }

    public ReadStrategyEnum ReadStrategy { get; set; } = ReadStrategyEnum.Loop;
    // public bool DefaultEnable { get; set; } = false;
    public int DefaultPriority { get; set; } = 10;

    public ReadWriteDefaultEnableBehavior DefaultEnableBehavior { get; set; } =
        ReadWriteDefaultEnableBehavior.DefaultDisable;

    public ReadConnStringGetStrategyEnum ReadConnStringGetStrategy { get; set; } =
        ReadConnStringGetStrategyEnum.LatestFirstTime;
}