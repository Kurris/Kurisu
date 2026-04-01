using Kurisu.Extensions.ContextAccessor.Abstractions;
using Microsoft.Extensions.Logging;

namespace Kurisu.Extensions.ContextAccessor;

/// <summary>
/// 上下文访问器抽象类
/// </summary>
/// <typeparam name="TState"></typeparam>
public abstract class AbstractContextAccessor<TState> : IContextAccessor<TState> where TState : class, new()
{
    private readonly ILogger _logger;

    protected AbstractContextAccessor(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 初始化上下文
    /// </summary>
    public virtual void Initialize()
    {
        Current = new TState();
        _logger.LogDebug("上下文'{StateType}'已初始化.", typeof(TState).FullName);
    }

    /// <summary>
    /// 移除上下文
    /// </summary>
    public virtual void Remove()
    {
        Current = null;
        _logger.LogDebug("上下文'{StateType}'已释放.", typeof(TState).FullName);
    }

    /// <summary>
    /// 当前上下文
    /// </summary>
    public abstract TState Current { get; set; }
}