using System.ComponentModel.DataAnnotations;
using Kurisu.RemoteCall.Abstractions;
using Kurisu.RemoteCall.Utils;

namespace Kurisu.RemoteCall.Default;

/// <summary>
/// 默认参数验证器
/// </summary>
internal class DefaultParameterValidator : IRemoteCallParameterValidator
{
    public void Validate(object[] values)
    {
        if (values == null || values.Length == 0) return;

        foreach (var item in values)
        {
            if (item == null) continue; // skip null values
            // Skip validation for simple/value types because Validator.ValidateObject expects complex objects
            if (!TypeHelper.IsReferenceType(item.GetType())) continue;

            Validator.ValidateObject(item, new ValidationContext(item), true);
        }
    }
}