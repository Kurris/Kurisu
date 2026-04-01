using System;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;

namespace Kurisu.AspNetCore.MultiLanguage;

/// <summary>
/// 自定义请求头多语言文化提供者。
/// 通过指定的请求头获取并设置当前请求的文化信息。
/// </summary>
public class CustomHeaderRequestCultureProvider : IRequestCultureProvider
{
    private readonly string _multiLanguageKey;
    private readonly HashSet<string> _supportedCultures = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _aliasMap = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 便捷构造函数：仅传入请求头键名和别名映射（使用默认支持文化）。
    /// 例如：new CustomHeaderRequestCultureProvider("X-Language", myAliasMap)
    /// </summary>
    /// <param name="multiLanguageKey">多语言请求头键名</param>
    /// <param name="aliasMap">别名映射</param>
    public CustomHeaderRequestCultureProvider(string multiLanguageKey, IDictionary<string, string> aliasMap)
    {
        if (string.IsNullOrEmpty(multiLanguageKey))
            throw new ArgumentNullException(nameof(multiLanguageKey));

        _multiLanguageKey = multiLanguageKey;
        aliasMap ??= new Dictionary<string, string>()
        {
            { "zh", "zh-CN" },
            { "en", "en-US" },
             // 可以添加更多默认别名映射
        };

        foreach (var kv in aliasMap)
        {
            if (string.IsNullOrWhiteSpace(kv.Key) || string.IsNullOrWhiteSpace(kv.Value)) continue;
            var ci = CultureInfo.GetCultureInfo(kv.Value);
            _aliasMap[kv.Key.Trim()] = ci.Name;
            _supportedCultures.Add(ci.Name);
        }
    }

    /// <summary>
    /// 根据请求头获取文化信息。
    /// </summary>
    /// <param name="httpContext">HTTP上下文</param>
    /// <returns>文化结果，若无则为null</returns>
    public Task<ProviderCultureResult> DetermineProviderCultureResult(HttpContext httpContext)
    {
        if (httpContext == null)
            throw new ArgumentNullException(nameof(httpContext));

        if (!httpContext.Request.Headers.TryGetValue(_multiLanguageKey, out var values))
        {
            return Task.FromResult<ProviderCultureResult>(null);
        }

        var headerValue = values.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(headerValue))
        {
            return Task.FromResult<ProviderCultureResult>(null);
        }

        // parse header: handle values like "en-US,en;q=0.9" or "en;q=0.8"
        var raw = headerValue.Split(',').First().Split(';').First().Trim();
        if (string.IsNullOrEmpty(raw)) return Task.FromResult<ProviderCultureResult>(null);

        // try alias map first (e.g. "zh" -> "zh-CN")
        if (_aliasMap.TryGetValue(raw, out var mapped))
        {
            if (_supportedCultures.Contains(mapped))
                return Task.FromResult(new ProviderCultureResult(mapped, mapped));
            // if mapped culture not in supported, still try to return mapped if valid
            return Task.FromResult(new ProviderCultureResult(mapped, mapped));
        }

        // try to normalize to a valid culture
        try
        {
            var ci = CultureInfo.GetCultureInfo(raw);
            var name = ci.Name;
            if (_supportedCultures.Contains(name))
            {
                return Task.FromResult(new ProviderCultureResult(name, name));
            }

            // if not explicitly in supported list, still return the culture (best-effort)
            return Task.FromResult(new ProviderCultureResult(name, name));
        }
        catch
        {
            // unknown culture string
        }

        return Task.FromResult<ProviderCultureResult>(null);
    }
}