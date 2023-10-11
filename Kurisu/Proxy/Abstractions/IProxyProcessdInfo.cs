using System.Reflection;

namespace Kurisu.Proxy.Abstractions;


public interface IProxyProcessdInfo
{
    void Invoke();
}


public interface IProxyInfo
{
    public object Target { get; set; }

    public MethodInfo Method { get; set; }

    public object[] Parameters { get; set; }

    object ReturnValue { get; set; }

    void Proceed();
}
