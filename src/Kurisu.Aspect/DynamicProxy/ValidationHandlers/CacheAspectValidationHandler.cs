using System.Collections.Concurrent;

namespace AspectCore.DynamicProxy;

public sealed class CacheAspectValidationHandler : IAspectValidationHandler
{
    private readonly ConcurrentDictionary<AspectValidationContext, bool> _detectorCache = new();

    public int Order { get; } = -101;

    public bool Invoke(AspectValidationContext context, AspectValidationDelegate next)
    {
        return _detectorCache.GetOrAdd(context, tuple => next(context));
    }
}