using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Kurisu.Utils
{
    public class Json
    {
        /// <summary>
        /// 转对象
        /// </summary>
        /// <param name="json"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T ToObject<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            });
        }
    }
}