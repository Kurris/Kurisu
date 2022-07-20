using Newtonsoft.Json;

namespace Kurisu.Utils.Extensions
{
    /// <summary>
    /// json扩展方法
    /// </summary>
    public static class JsonExtensions
    {
        /// <summary>
        /// 反序列化为对象
        /// </summary>
        /// <param name="str"></param>
        /// <param name="settings"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T ToObject<T>(this string str, JsonSerializerSettings settings = default)
        {
            settings ??= new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };
            return JsonConvert.DeserializeObject<T>(str, settings);
        }

        /// <summary>
        /// 序列化对象为字符串
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static string ToJson(this object obj, JsonSerializerSettings settings = default)
        {
            settings ??= new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };
            return JsonConvert.SerializeObject(obj, settings);
        }
    }
}