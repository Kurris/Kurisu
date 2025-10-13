using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.RemoteCall.Extensions;

internal class RemoteCallBuilder : IRemoteCallBuilder
{
    /// <inheritdoc />
    public IServiceCollection Services { get; set; }
}