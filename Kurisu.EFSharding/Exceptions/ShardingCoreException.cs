using System.Diagnostics.CodeAnalysis;

namespace Kurisu.EFSharding.Exceptions;


[ExcludeFromCodeCoverage]
public class ShardingCoreException : Exception
{
    public ShardingCoreException()
    {

    }
    public ShardingCoreException(string message) : base(message)
    {
    }

    public ShardingCoreException(string message, Exception innerException) : base(message, innerException)
    {
    }
}