namespace Kurisu.AspNetCore.CustomClass;

/// <summary>
/// ip:port
/// </summary>
public record IpPortModel
{
    /// <summary>
    /// ip
    /// </summary>
    public string Ip { get; set; }

    /// <summary>
    /// port
    /// </summary>
    public int Port { get; set; }
}