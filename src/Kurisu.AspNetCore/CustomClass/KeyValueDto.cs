namespace Kurisu.AspNetCore.CustomClass;

/// <summary>
/// 键对值
/// </summary>
/// <typeparam name="TKey"></typeparam>
/// <typeparam name="TValue"></typeparam>
public struct KeyValueDto<TKey, TValue>
{
    /// <summary>
    /// key
    /// </summary>
    public TKey Key { get; set; }

    /// <summary>
    /// value
    /// </summary>
    public TValue Value { get; set; }
}