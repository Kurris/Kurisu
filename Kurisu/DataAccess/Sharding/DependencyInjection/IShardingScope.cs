using System;

namespace Kurisu.DataAccess.Sharding.DependencyInjection;

public interface IShardingScope : IDisposable
{
    IShardingProvider ServiceProvider { get; }
}