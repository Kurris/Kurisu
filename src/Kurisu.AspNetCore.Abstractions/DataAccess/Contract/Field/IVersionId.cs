namespace Kurisu.AspNetCore.Abstractions.DataAccess.Contract.Field;

/// <summary>
/// 版本
/// </summary>
public interface IVersionId
{
    /// <summary>
    /// 数据版本号
    /// </summary>
    public int Version { get; set; }
}