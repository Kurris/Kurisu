using System.Collections.Generic;
using System.Threading.Tasks;

namespace Kurisu.AspNetCore.MultiLanguage.Abstractions;

/// <summary>
/// 本地化处理器
/// </summary>
public interface ILocalizationHandler
{
    /// <summary>
    /// 获取值
    /// </summary>
    /// <param name="key"></param>
    /// <param name="cultureCode"></param>
    /// <returns></returns>
    Task<string> GetAsync(string key, string cultureCode);

    /// <summary>
    /// 获取所有值
    /// </summary>
    /// <param name="cultureCode"></param>
    /// <returns></returns>
    Task<Dictionary<string, string>> GetAllAsync(string cultureCode);
}