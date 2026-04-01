using Kurisu.Extensions.ContextAccessor.Abstractions;
using Kurisu.Extensions.ContextAccessor.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kurisu.Extensions.ContextAccessor;

/// <summary>
/// 状态访问器服务注册扩展
/// </summary>
public static class ContextAccessorServiceCollectionExtensions
{
    /// <summary>
    /// 添加状态访问器 
    /// </summary>
    /// <param name="services"></param>
    /// <typeparam name="TState"></typeparam>
    public static ContextAccessorBuilder<TState> AddContextAccessor<TState>(this IServiceCollection services) where TState : class, IContextable<TState>, new()
    {
        // 将接口都映射到上面那个具体实例（通过工厂返回同一个对象）
        services.TryAddSingleton<IContextAccessor<TState>, ContextAccessor<TState>>();
        return new ContextAccessorBuilder<TState>(services).WithLifecycle();
    }
}
