using Kurisu.AspNetCore.Abstractions.State;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kurisu.AspNetCore.State.Extensions;

/// <summary>
/// 状态访问器服务注册扩展
/// </summary>
public static class StateAccessorServiceCollectionExtensions
{
    /// <summary>
    /// 添加状态访问器 
    /// </summary>
    /// <param name="services"></param>
    /// <typeparam name="TState"></typeparam>
    public static void AddStateAccessor<TState>(this IServiceCollection services) where TState : class, ICopyable<TState>, new()
    {
        services.TryAddSingleton<IStateAccessor<TState>, StateAccessor<TState>>();
        services.TryAddSingleton(typeof(IStateLifecycle), typeof(StateAccessor<TState>));
        services.TryAddSingleton(typeof(IStateSnapshotManager<TState>), typeof(StateSnapshotManager<TState>));
    }
}