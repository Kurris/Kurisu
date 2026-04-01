using System.Threading;
using Microsoft.Extensions.Logging;

namespace Kurisu.Extensions.ContextAccessor.Internal;

/// <summary>
/// 为每个异步上下文提供一个轻量的每请求状态访问器，类型为 <typeparamref name="TState"/>。
/// </summary>
/// <remarks>
/// 状态存储在 <see cref="AsyncLocal{T}"/> 的 Holder 中，行为参考 <see cref="IHttpContextAccessor"/> 的实现方式。
/// </remarks>
internal class ContextAccessor<TState> : AbstractContextAccessor<TState> where TState : class, new()
{
    private static readonly AsyncLocal<StateHolder> _stateCurrent = new();

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="logger"></param>
    public ContextAccessor(ILogger<ContextAccessor<TState>> logger) : base(logger)
    {
    }

    /// <summary>
    /// 当前异步上下文的状态（可读写）。语义参考 IHttpContextAccessor：getter 返回当前值（可能为 null），setter 设置/创建 Holder.State。
    /// </summary>
    public override TState Current
    {
        get => _stateCurrent.Value?.State;
        set
        {
            var holder = _stateCurrent.Value;
            if (holder != null)
            {
                holder.State = null;
            }

            if (value != null)
            {
                _stateCurrent.Value = new StateHolder { State = value };
            }
        }
    }

    private sealed class StateHolder
    {
        public TState State;
    }
}