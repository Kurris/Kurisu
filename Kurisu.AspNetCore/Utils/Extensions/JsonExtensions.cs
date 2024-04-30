using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Kurisu.AspNetCore.Utils.Extensions;

/// <summary>
/// json扩展方法
/// </summary>
public static class JsonExtensions
{
    /// <summary>
    /// 默认json配置
    /// </summary>
    public static readonly JsonSerializerSettings DefaultSetting = new()
    {
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        DateFormatString = "yyyy-MM-dd HH:mm:ss"
    };

    /// <summary>
    /// 反序列化
    /// </summary>
    /// <param name="json">json</param>
    /// <param name="settings">序列化配置<see cref="DefaultSetting"/></param>
    /// <typeparam name="T">序列化类型</typeparam>
    /// <returns></returns>
    public static T ToObject<T>(this string json, JsonSerializerSettings settings = default)
    {
        return JsonConvert.DeserializeObject<T>(json, settings);
    }

    /// <summary>
    /// 序列化
    /// </summary>
    /// <param name="obj">对象</param>
    /// <param name="settings">序列化配置</param>
    /// <returns></returns>
    public static string ToJson<T>(this T obj, JsonSerializerSettings settings = default)
    {
        return JsonConvert.SerializeObject(obj, settings);
    }
}