using System.Threading;
using System.Threading.Tasks;
using Kurisu.AspNetCore.Abstractions.Cache;

namespace Kurisu.Extensions.Cache.Locking;

public sealed class NoRetryDistributedLockRetryStrategy : AbstractDistributedLockRetryStrategy
{
    public NoRetryDistributedLockRetryStrategy()
        : base(null, 0)
    {
    }

    public override Task<bool> ShouldRetryAsync(int attempt, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(false);
    }
}
