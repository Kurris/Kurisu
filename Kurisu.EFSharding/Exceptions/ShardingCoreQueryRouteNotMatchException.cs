using System.Diagnostics.CodeAnalysis;

namespace Kurisu.EFSharding.Exceptions;

[ExcludeFromCodeCoverage]
public class ShardingCoreQueryRouteNotMatchException : ShardingCoreException
{
    public ShardingCoreQueryRouteNotMatchException(string message) : base(message)
    {
    }

    public ShardingCoreQueryRouteNotMatchException(string message, Exception innerException) : base(message, innerException)
    {
    }
}