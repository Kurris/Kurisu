using System.Threading;
using Kurisu.AspNetCore.Abstractions.Cache;

namespace Kurisu.Extensions.Cache.Locking;

internal interface ILocalReentryAwareLockHandler : ILockHandler
{
    ValueTask<bool> TryReenterAsync(CancellationToken cancellationToken = default);
}
