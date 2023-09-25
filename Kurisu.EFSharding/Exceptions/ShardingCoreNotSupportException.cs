using System.Diagnostics.CodeAnalysis;

namespace Kurisu.EFSharding.Exceptions;

[ExcludeFromCodeCoverage]
public class ShardingCoreNotSupportException : ShardingCoreException
{
    public ShardingCoreNotSupportException(string message) : base(message)
    {
    }

    public ShardingCoreNotSupportException(string message, Exception innerException) : base(message, innerException)
    {
    }
}