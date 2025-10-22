using System.Collections;
using System.Reflection;
using System.Text;
using Kurisu.RemoteCall.Attributes;
using Newtonsoft.Json;

namespace Kurisu.RemoteCall.Utils;

internal static class HttpContentUtils
{
    public static HttpContent Create(MethodInfo method, List<ParameterValue> parameters)
    {
        var postAttribute = method.GetCustomAttribute<PostAttribute>()!;
        if (parameters.Count == 0)
        {
            return new StringContent("{}", Encoding.UTF8, postAttribute.ContentType);
        }

        var parameterValue = parameters[0];
        var isStringType = parameterValue.Parameter.ParameterType == typeof(string);
        var value = parameterValue.Value;

        if (postAttribute.IsUrlencoded)
        {
            //实体参数
            if (isStringType)
            {
                return new StringContent(value.ToString()!, Encoding.UTF8, postAttribute.ContentType);
            }

            var dict = ToStringDictionary(value);
            return new FormUrlEncodedContent(dict);
        }

        if (isStringType)
        {
            return new StringContent(value.ToString()!, Encoding.UTF8, postAttribute.ContentType);
        }

        return new StringContent(JsonConvert.SerializeObject(value), Encoding.UTF8, postAttribute.ContentType);
    }

    private static Dictionary<string, string> ToStringDictionary(object obj)
    {
        if (obj == null) return new Dictionary<string, string>();

        // 已经是 Dictionary<string,string>
        if (obj is Dictionary<string, string> dd) return new Dictionary<string, string>(dd);

        // 如果是集合（除了 IDictionary），不支持表单转换，返回空字典
        if (obj is IEnumerable and not IDictionary)
        {
            return new Dictionary<string, string>();
        }

        // 支持非泛型 IDictionary
        if (obj is IDictionary dict)
        {
            var result = new Dictionary<string, string>();
            foreach (DictionaryEntry de in dict)
            {
                var key = de.Key.ToString()!;
                result[key] = de.Value?.ToString() ?? string.Empty;
            }

            return result;
        }

        // 尝试通过 JSON 序列化/反序列化为字典
        try
        {
            var d = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(obj));
            if (d != null) return d;
        }
        catch
        {
            // ignore
        }

        // 反射取属性
        var dictResult = new Dictionary<string, string>();
        var props = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var p in props)
        {
            var v = p.GetValue(obj);
            dictResult[p.Name] = v?.ToString() ?? string.Empty;
        }

        return dictResult;
    }
}