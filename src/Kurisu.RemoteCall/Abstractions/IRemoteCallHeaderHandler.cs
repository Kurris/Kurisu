namespace Kurisu.RemoteCall.Abstractions;

/// <summary>
/// 结果处理器
/// </summary>
public interface IRemoteCallHeaderHandler : IRemoteCallHandler
{
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    Dictionary<string, string> Handler();
}