using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Kurisu.Utils.Extensions;

/// <summary>
/// json扩展方法
/// </summary>
public static class JsonExtensions
{
    /// <summary>
    /// 反序列化为对象
    /// </summary>
    /// <remarks>
    /// ReferenceLoopHandling.Ignore , CamelCasePropertyNamesContractResolver
    /// </remarks>
    /// <param name="json">json</param>
    /// <param name="settings">序列化配置</param>
    /// <typeparam name="T">序列化类型</typeparam>
    /// <returns></returns>
    public static T ToObject<T>(this string json, JsonSerializerSettings settings = default)
    {
        settings ??= new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        return JsonConvert.DeserializeObject<T>(json, settings);
    }

    /// <summary>
    /// 序列化对象为字符串
    /// </summary>
    /// <remarks>
    /// ReferenceLoopHandling.Ignore , CamelCasePropertyNamesContractResolver , yyyy-MM-dd HH:mm:ss
    /// </remarks>
    /// <param name="obj">对象</param>
    /// <param name="settings">序列化配置</param>
    /// <returns></returns>
    public static string ToJson(this object obj, JsonSerializerSettings settings = default)
    {
        settings ??= new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            DateFormatString = "yyyy-MM-dd HH:mm:ss"
        };
        return JsonConvert.SerializeObject(obj, settings);
    }
}