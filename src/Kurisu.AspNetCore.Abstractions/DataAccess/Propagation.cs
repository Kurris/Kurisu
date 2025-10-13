namespace Kurisu.AspNetCore.Abstractions.DataAccess;

public enum Propagation
{
    /// <summary>
    /// 如果存在事务则加入当前事务，否则新建事务。常用选项。
    /// </summary>
    Required,

    /// <summary>
    /// 始终新建事务,独立提交/回滚。
    /// </summary>
    RequiresNew,

    /// <summary>
    /// 必须在事务中运行，否则抛出异常。
    /// </summary>
    Mandatory,

    /// <summary>
    /// 不允许在事务中运行，若存在事务则抛出异常。
    /// </summary>
    Never,

    /// <summary>
    /// 若存在事务则在嵌套事务中执行，否则行为同 <see cref="Required"/>。
    /// </summary>
    Nested
}