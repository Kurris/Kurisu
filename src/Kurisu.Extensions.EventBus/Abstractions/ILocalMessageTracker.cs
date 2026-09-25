namespace Kurisu.Extensions.EventBus.Abstractions;

/// <summary>
/// 本地消息追踪器，await using 退出时写回结果；未标记结果时不更新消息。
/// 重复释放不重复写回，释放后不可再标记结果。
/// </summary>
public interface ILocalMessageTracker : IAsyncDisposable
{
    /// <summary>
    /// 标记处理成功；已标记失败时仍以失败为准。
    /// </summary>
    void Complete();

    /// <summary>
    /// 标记处理失败并记录错误信息；错误信息为空也视为失败。
    /// </summary>
    /// <param name="error">错误信息</param>
    void Fail(string error);
}
