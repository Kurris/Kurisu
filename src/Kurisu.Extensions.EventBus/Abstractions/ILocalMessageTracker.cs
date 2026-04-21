namespace Kurisu.Extensions.EventBus.Abstractions;

/// <summary>
/// 本地消息追踪器，using 模式管理消息处理状态
/// </summary>
public interface ILocalMessageTracker : IAsyncDisposable
{
    /// <summary>
    /// 标记处理成功
    /// </summary>
    void Complete();

    /// <summary>
    /// 标记处理失败并记录错误信息
    /// </summary>
    /// <param name="error">错误信息</param>
    void Fail(string error);
}
