using System.Data;

namespace Kurisu.AspNetCore.Abstractions.DataAccess.Core;

/// <summary>
/// 事务管理器接口
/// </summary>
public interface ITransactionManager
{
    /// <summary>
    /// 创建事务作用域
    /// </summary>
    /// <param name="propagation">事务传播性</param>
    /// <param name="isolationLevel">隔离级别</param>
    /// <returns></returns>
    ITransactionScope CreateTransScope(Propagation propagation, IsolationLevel? isolationLevel = null);
}