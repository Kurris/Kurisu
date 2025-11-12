using Kurisu.AspNetCore.Abstractions.State;

namespace Kurisu.AspNetCore.State;

/// <summary>
/// 状态访问器抽象类
/// </summary>
/// <typeparam name="TState"></typeparam>
public abstract class AbstractStateAccessor<TState> : IStateAccessor<TState> where TState : class, new()
{
    /// <summary>
    /// 初始化状态
    /// </summary>
    public virtual void Initialize()
    {
        Current = new TState();
    }

    /// <summary>
    /// 移除状态
    /// </summary>
    public virtual void Remove()
    {
        Current = null;
    }

    /// <summary>
    /// 当前状态
    /// </summary>
    public abstract TState Current { get; set; }
}