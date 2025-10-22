using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
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
            ValidateObject(item);
        }
    }

    /// <summary>
    /// 验证实体
    /// </summary>
    /// <param name="obj"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ValidateObject(object obj)
    {
        var type = obj.GetType();
        if (type.IsGenericType)
        {
            if (obj is not IEnumerable enumerable) return;
            foreach (var item in enumerable)
            {
                ValidateObject(item);
            }
        }
        else
        {
            Validator.ValidateObject(obj, new ValidationContext(obj), true);
        }
    }
}