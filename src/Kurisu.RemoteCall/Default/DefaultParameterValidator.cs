using System.ComponentModel.DataAnnotations;
using Kurisu.RemoteCall.Abstractions;

namespace Kurisu.RemoteCall.Default;

/// <summary>
/// 默认参数验证器
/// </summary>
internal class DefaultParameterValidator : IRemoteCallParameterValidator
{
    public void Validate(object[] values)
    {
        foreach (var item in values)
        {
            Validator.ValidateObject(item, new ValidationContext(item), true);
        }
    }
}