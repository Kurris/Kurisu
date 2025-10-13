namespace Kurisu.RemoteCall.Abstractions;

/// <summary>
/// 
/// </summary>
public interface IRemoteCallParameterValidator
{
    /// <summary>
    /// 验证
    /// </summary>
    /// <param name="parameters"></param>
    void Validate(List<ParameterValue> parameters);
}