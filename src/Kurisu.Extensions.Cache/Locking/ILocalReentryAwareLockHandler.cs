using System.Threading;
using Kurisu.AspNetCore.Abstractions.DistributedLock;

namespace Kurisu.Extensions.Cache.Locking;

internal interface ILocalReentryAwareLockHandler : ILockHandler
{
    ValueTask<bool> TryReenterAsync(CancellationToken cancellationToken = default);
}
