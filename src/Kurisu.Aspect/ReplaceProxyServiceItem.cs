using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Aspect;

public class ReplaceProxyServiceItem
{
    public ServiceLifetime Lifetime { get; set; }

    public Type Service { get; set; }

    public Type[] InterfaceTypes { get; set; }

    public string Named { get; set; }
}