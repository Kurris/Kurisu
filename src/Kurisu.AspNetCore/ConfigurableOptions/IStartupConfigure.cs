namespace Kurisu.AspNetCore.ConfigurableOptions;

/// <summary>
/// 启动项配置
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IStartupConfigure<in T> where T : class
{
    /// <summary>
    /// 配置
    /// </summary>
    /// <param name="value"></param>
    void StartupConfigure(T value);
}