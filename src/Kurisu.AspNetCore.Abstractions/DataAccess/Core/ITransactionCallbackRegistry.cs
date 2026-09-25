namespace Kurisu.AspNetCore.Abstractions.DataAccess.Core;

/// <summary>
/// 事务回调注册器。
/// </summary>
public interface ITransactionCallbackRegistry
{
    /// <summary>是否存在活动事务。未实现此能力的适配器保守返回 true，查询缓存将绕过。</summary>
    bool HasActiveTransaction => true;

    /// <summary>
    /// 注册事务提交后回调。当前无活动事务时立即执行。
    /// </summary>
    /// <param name="callback">提交后执行的回调。</param>
    /// <returns></returns>
    Task RegisterAfterCommitAsync(Func<Task> callback);
}
