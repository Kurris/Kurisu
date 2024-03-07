using System.Collections.Generic;

namespace Kurisu.AspNetCore.Utils;

/// <summary>
/// 可递归结构
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IRecursionStruct<T>
{
    /// <summary>
    /// 父级
    /// </summary>
    public string PCode { get; set; }

    /// <summary>
    /// 当前
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// 下级
    /// </summary>
    public List<T> Next { get; set; }
}
