namespace Kurisu.EFSharding.Core.DependencyInjection;

public interface IShardingScope : IDisposable
{
    IShardingProvider ServiceProvider { get; }
}