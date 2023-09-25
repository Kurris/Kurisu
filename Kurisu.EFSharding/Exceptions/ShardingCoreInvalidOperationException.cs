using System.Diagnostics.CodeAnalysis;

namespace Kurisu.EFSharding.Exceptions;

[ExcludeFromCodeCoverage]
public class ShardingCoreInvalidOperationException: ShardingCoreException
{

    public ShardingCoreInvalidOperationException(string? message) : base(message)
    {
    }

    public ShardingCoreInvalidOperationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}