namespace Kurisu.AspNetCore.Abstractions.Startup;

/// <summary>
/// 提供应用启动时需要额外加载的 JSON 配置文件。
/// </summary>
public interface IJsonConfigurationProvider
{
    /// <summary>
    /// 配置加载顺序，值越小越先加载。
    /// </summary>
    int Order => 100;

    /// <summary>
    /// 获取当前环境需要加载的 JSON 配置文件。
    /// </summary>
    /// <param name="environmentName">当前环境名称。</param>
    /// <returns>JSON 配置文件列表。</returns>
    IEnumerable<JsonConfigurationFile> GetJsonFiles(string environmentName);
}
