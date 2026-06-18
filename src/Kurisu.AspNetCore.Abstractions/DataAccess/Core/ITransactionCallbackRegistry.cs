namespace Kurisu.AspNetCore.Abstractions.DataAccess.Core;

/// <summary>
/// 事务回调注册器。
/// </summary>
public interface ITransactionCallbackRegistry
{
    /// <summary>
    /// 注册事务提交后回调。当前无活动事务时立即执行。
    /// </summary>
    /// <param name="callback">提交后执行的回调。</param>
    /// <returns></returns>
    Task RegisterAfterCommitAsync(Func<Task> callback);
}
