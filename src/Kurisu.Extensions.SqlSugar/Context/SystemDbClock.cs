namespace Kurisu.Extensions.SqlSugar.Context;

/// <summary>
/// 基于系统本地时间的数据库审计时间访问器。
/// </summary>
public class SystemDbClock : IDbClock
{
    public DateTime Now => DateTime.Now;
}
