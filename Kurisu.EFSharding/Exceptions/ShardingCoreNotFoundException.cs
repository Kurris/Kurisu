using System.Diagnostics.CodeAnalysis;

namespace Kurisu.EFSharding.Exceptions;

[ExcludeFromCodeCoverage]
public class ShardingCoreNotFoundException:ShardingCoreException
{
    public ShardingCoreNotFoundException(string message) : base(message)
    {
    }

    public ShardingCoreNotFoundException(string message, Exception innerException) : base(message, innerException)
    {
    }
}