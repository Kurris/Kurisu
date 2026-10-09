using Kurisu.AspNetCore.Abstractions.Utils.Disposables;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.Abstractions.Startup;

/// <summary>
/// 初始化和移除生命周期接口
/// </summary>
public interface IAppAsyncLocalLifecycle
{
    /// <summary>
    /// 当前上下文是否已初始化. 默认值兼容旧实现, 避免在嵌套调用时重置已有状态.
    /// </summary>
    bool IsInitialized => true;

    /// <summary>
    /// 初始化
    /// </summary>
    void Initialize();

    /// <summary>
    /// 移除销毁
    /// </summary>
    void Remove();
}

/// <summary>
/// AppExtensions
/// </summary>
public static class AppExtensions
{
    /// <summary>
    /// 仅初始化当前缺失的生命周期状态, 退出时只移除本次创建的状态.
    /// 后台任务可独立使用, 已处于 HTTP 生命周期的调用不会被重置.
    /// </summary>
    public static IDisposable EnsureLifecycle(this IServiceProvider serviceProvider)
    {
        var initialized = new List<IAppAsyncLocalLifecycle>();
        try
        {
            foreach (var item in serviceProvider.GetServices<IAppAsyncLocalLifecycle>())
            {
                if (item.IsInitialized) continue;
                item.Initialize();
                initialized.Add(item);
            }
        }
        catch
        {
            foreach (var item in initialized.AsEnumerable().Reverse()) item.Remove();
            throw;
        }
        return new ActionScope(() =>
        {
            foreach (var item in initialized.AsEnumerable().Reverse()) item.Remove();
        });
    }

    /// <summary>
    /// 确保IStateLifecycle在作用域中正确初始化与移除
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <returns></returns>
    public static IDisposable InitLifecycle(this IServiceProvider serviceProvider)
    {
        var stateLifecycles = serviceProvider.GetServices<IAppAsyncLocalLifecycle>();
        foreach (var item in stateLifecycles)
        {
            item.Initialize();
        }

        return new ActionScope(() =>
        {
            foreach (var item in stateLifecycles.Reverse())
            {
                item.Remove();
            }
        });
    }
}
