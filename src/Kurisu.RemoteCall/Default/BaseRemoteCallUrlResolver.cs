using Kurisu.RemoteCall.Abstractions;
using Microsoft.Extensions.Configuration;

namespace Kurisu.RemoteCall.Default;

/// <summary>
/// Url处理器基类，实现配置占位符替换和Url拼接
/// </summary>
public abstract class BaseRemoteCallUrlResolver : IRemoteCallUrlResolver
{
    /// <summary>
    /// 配置对象
    /// </summary>
    protected IConfiguration Configuration { get; }

    /// <summary>
    /// 构造函数，注入配置
    /// </summary>
    /// <param name="configuration">配置对象</param>
    protected BaseRemoteCallUrlResolver(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    /// <summary>
    /// 获取完整Url
    /// </summary>
    /// <param name="httpMethod">请求方法</param>
    /// <param name="baseUrl">基础Url</param>
    /// <param name="template">模板Url</param>
    /// <param name="parameters">参数列表</param>
    /// <returns>完整Url</returns>
    public string ResolveUrl(string httpMethod, string baseUrl, string template, List<ParameterValue> parameters)
    {
        baseUrl = ResolveBaseUrl(baseUrl);
        template = ResolveTemplateUrl(httpMethod, template, parameters);

        if (string.IsNullOrEmpty(template))
            return baseUrl ?? string.Empty;

        if (template.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return template;

        if (string.IsNullOrEmpty(baseUrl))
            return template;

        // 保证拼接时不会出现双斜杠或漏斜杠
        if (!baseUrl.EndsWith('/') && !template.StartsWith('/'))
            return baseUrl + "/" + template;
        if (baseUrl.EndsWith('/') && template.StartsWith('/'))
            return baseUrl.TrimEnd('/') + template;
        return baseUrl + template;
    }

    /// <summary>
    /// 处理baseUrl
    /// </summary>
    /// <param name="baseUrl">基础Url</param>
    /// <returns>处理后的基础Url</returns>
    protected virtual string ResolveBaseUrl(string baseUrl)
    {
        return FixFromConfiguration(baseUrl);
    }

    /// <summary>
    /// 处理模板Url
    /// </summary>
    /// <param name="httpMethod">请求方法</param>
    /// <param name="template">模板Url</param>
    /// <param name="parameters">参数列表</param>
    /// <returns>处理后的模板Url</returns>
    protected virtual string ResolveTemplateUrl(string httpMethod, string template, List<ParameterValue> parameters)
    {
        return FixFromConfiguration(template);
    }

    /// <summary>
    /// 从配置中替换占位符
    /// </summary>
    /// <param name="str">待处理字符串</param>
    /// <returns>替换后的字符串</returns>
    private string FixFromConfiguration(string str)
    {
        if (string.IsNullOrEmpty(str))
            return str;

        var start = str.IndexOf("${", StringComparison.Ordinal);
        while (start >= 0)
        {
            var end = str.IndexOf('}', start);
            if (end < 0)
                break;

            var config = str.Substring(start, end - start + 1);
            var path = config.Substring(2, config.Length - 3).Trim();
            var configValue = Configuration.GetSection(path).Value ?? string.Empty;
            str = str.Replace(config, configValue);

            start = str.IndexOf("${", StringComparison.Ordinal);
        }

        return str;
    }
}