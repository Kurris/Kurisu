using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Aspect;

public class ReplaceProxyServiceItem
{
    public ServiceLifetime Lifetime { get; set; }

    public Type Service { get; set; }
}