using Kurisu.EFSharding.Sharding.Abstractions;
using Kurisu.EFSharding.Extensions;

namespace Kurisu.EFSharding.Sharding.ShardingExecutors;

internal class CustomerReadWriteScope:IDisposable
{
    private readonly IShardingDbContext _shardingDbContext;
    private readonly bool _readOnly;

    public CustomerReadWriteScope(IShardingDbContext shardingDbContext,bool readOnly)
    {
        _shardingDbContext = shardingDbContext;
        _readOnly = readOnly;
        if (_readOnly)
        {
            _shardingDbContext.ReadWriteSeparationReadOnly();
        }
        else
        {
            _shardingDbContext.ReadWriteSeparationWriteOnly();
        }
    }

    public void Dispose()
    {
        if (_readOnly)
        {
            _shardingDbContext.ReadWriteSeparationWriteOnly();
        }
        else
        {
            _shardingDbContext.ReadWriteSeparationReadOnly();
        }
    }
}