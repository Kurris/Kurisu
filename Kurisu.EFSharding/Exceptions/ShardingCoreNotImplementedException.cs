using System.Diagnostics.CodeAnalysis;

namespace Kurisu.EFSharding.Exceptions;

[ExcludeFromCodeCoverage]
public class ShardingCoreNotImplementedException:ShardingCoreException
{
    public ShardingCoreNotImplementedException()
    {

    }
    public ShardingCoreNotImplementedException(string message) : base(message)
    {
    }

    public ShardingCoreNotImplementedException(string message, Exception innerException) : base(message, innerException)
    {
    }
}