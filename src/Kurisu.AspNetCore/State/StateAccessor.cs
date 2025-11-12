using System.Threading;

namespace Kurisu.AspNetCore.State;

/// <summary>
/// 为每个异步上下文提供一个轻量的每请求状态访问器，类型为 <typeparamref name="TState"/>。
/// 状态存储在 <see cref="AsyncLocal{T}"/> 的 Holder 中，行为参考 IHttpContextAccessor 的实现方式。
/// </summary>
public class StateAccessor<TState> : AbstractStateAccessor<TState> where TState : class, new()
{
    private static readonly AsyncLocal<StateHolder> _stateCurrent = new();

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