namespace Kurisu.Extensions.SqlSugar.Context;

/// <summary>
/// 数据库审计时间访问器。
/// </summary>
public interface IDbClock
{
    /// <summary>
    /// 获取当前数据库审计时间。
    /// </summary>
    DateTime Now { get; }
}
