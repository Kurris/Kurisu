namespace Kurisu.EFSharding.Sharding.Abstractions;

public interface IReadWriteSeparation
{
    int ReadWriteSeparationPriority { get; set; }
    bool ReadWriteSeparation { get; set; }
}