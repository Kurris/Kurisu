using Kurisu.EFSharding.Sharding.ReadWriteConfigurations;

namespace Kurisu.EFSharding.Sharding.Abstractions;

public interface IReadWriteSwitch
{
    int ReadWriteSeparationPriority { get; set; }
    bool ReadWriteSeparation { get; set; }
    ReadWriteDefaultEnableBehavior ReadWriteSeparationBehavior { get; set; }
}