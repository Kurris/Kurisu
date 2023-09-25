using System.Diagnostics.CodeAnalysis;

namespace Kurisu.EFSharding.Exceptions;

[ExcludeFromCodeCoverage]
public class ShardingCoreConfigException : ShardingCoreException
{
    public ShardingCoreConfigException(string message) : base(message)
    {
    }

    public ShardingCoreConfigException(string message, Exception innerException) : base(message, innerException)
    {
    }
}