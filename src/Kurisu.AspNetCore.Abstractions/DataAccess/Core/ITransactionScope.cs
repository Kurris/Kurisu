namespace Kurisu.AspNetCore.Abstractions.DataAccess.Core;

/// <summary>
///事务作用域接口
/// </summary>
public interface ITransactionScope : IDisposable
{
    /// <summary>
    /// 开始事务
    /// </summary>
    /// <returns></returns>
    Task BeginAsync();

    /// <summary>
    /// 提交事务
    /// </summary>
    /// <returns></returns>
    Task CommitAsync();

    /// <summary>
    /// 回滚事务
    /// </summary>
    /// <returns></returns>
    Task RollbackAsync();
}
