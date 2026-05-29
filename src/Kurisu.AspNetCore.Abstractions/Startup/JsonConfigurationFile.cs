namespace Kurisu.AspNetCore.Abstractions.Startup;

/// <summary>
/// JSON 配置文件描述。
/// </summary>
public sealed class JsonConfigurationFile
{
    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="path">JSON 配置文件路径。</param>
    /// <param name="optional">文件不存在时是否忽略。</param>
    /// <param name="reloadOnChange">文件变更时是否重新加载。</param>
    /// <exception cref="ArgumentException">配置文件路径为空。</exception>
    /// <exception cref="InvalidOperationException">配置文件不是 .json 文件。</exception>
    public JsonConfigurationFile(string path, bool optional = true, bool reloadOnChange = true)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("JSON 配置文件路径不能为空。", nameof(path));
        }

        if (!path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("JSON 配置文件路径必须以 .json 结尾。");
        }

        Path = path;
        Optional = optional;
        ReloadOnChange = reloadOnChange;
    }

    /// <summary>
    /// JSON 配置文件路径。
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// 文件不存在时是否忽略。
    /// </summary>
    public bool Optional { get; }

    /// <summary>
    /// 文件变更时是否重新加载。
    /// </summary>
    public bool ReloadOnChange { get; }
}
