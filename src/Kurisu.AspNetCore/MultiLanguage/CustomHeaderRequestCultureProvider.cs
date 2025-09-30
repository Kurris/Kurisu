using System;
using System.Linq;
using System.Threading.Tasks;
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

    /// <summary>
    /// 构造函数，初始化请求头键名。
    /// </summary>
    /// <param name="multiLanguageKey">多语言请求头键名</param>
    public CustomHeaderRequestCultureProvider(string multiLanguageKey)
    {
        _multiLanguageKey = multiLanguageKey;
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

        var supportedCultures = new[] { "en-US", "zh-CN", "fr-FR", "de-DE" };
        if (supportedCultures.Contains(headerValue))
        {
            return Task.FromResult(new ProviderCultureResult(headerValue, headerValue));
        }

        return Task.FromResult<ProviderCultureResult>(null);
    }
}