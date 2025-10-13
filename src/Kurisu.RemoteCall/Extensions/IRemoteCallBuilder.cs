// ReSharper disable once CheckNamespace

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// remote call builder
/// </summary>
public interface IRemoteCallBuilder
{
    /// <summary>
    /// ServiceCollection
    /// </summary>
    public IServiceCollection Services { get; set; }
}