namespace Kurisu.RemoteCall.Abstractions;

/// <summary>
/// 
/// </summary>
public interface IRemoteCallParameterValidator
{
    /// <summary>
    /// 验证
    /// </summary>
    /// <param name="values"></param>
    void Validate(object[] values);
}