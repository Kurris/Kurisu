namespace Kurisu.Extensions.EventBus.Abstractions;

/// <summary>
/// 死信处理服务，提供查询、忽略死信消息的能力。
/// </summary>
public interface IEventBusDeadLetterService
{
    /// <summary>
    /// 根据 code 查询死信消息详情。
    /// </summary>
    Task<LocalMessage> GetAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// 忽略该死信消息，必须填写原因。
    /// </summary>
    Task IgnoreAsync(string code, string reason, CancellationToken cancellationToken = default);
}
