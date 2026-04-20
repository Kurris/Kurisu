using Kurisu.AspNetCore.Abstractions.Utils.Disposables;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.Abstractions.Startup;

/// <summary>
/// 初始化和移除生命周期接口
/// </summary>
public interface IAppAsyncLocalLifecycle
{
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