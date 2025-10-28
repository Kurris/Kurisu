using System.Net;
using System.Reflection;
using System.Text;
using Kurisu.RemoteCall.Abstractions;
using Kurisu.RemoteCall.Attributes;

namespace Kurisu.RemoteCall.Utils;

internal static class HttpContentUtils
{
    public static HttpContent Create(MethodInfo method, List<ParameterValue> parameters, IJsonSerializer jsonSerializer, ICommonUtils commonUtils)
    {
        var postAttribute = method.GetCustomAttribute<PostAttribute>();
        var contentType = postAttribute?.ContentType ?? "application/json";
        var asUrlencoded = postAttribute?.AsUrlencodedFormat ?? false;

        if (parameters == null || parameters.Count == 0)
        {
            return new StringContent("{}", Encoding.UTF8, contentType);
        }

        var firstParam = parameters[0];
        var isStringType = firstParam.Parameter.ParameterType == typeof(string);
        var value = firstParam.Value;

        if (asUrlencoded)
        {
            var dict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            // special-case: only one string parameter and user provided the raw key=value string
            if (parameters.Count == 1 && isStringType)
            {
                var raw = value?.ToString() ?? string.Empty;

                if (string.Equals(contentType, "application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("When contentType is application/x-www-form-urlencoded and asUrlencodedFormat=true, the first parameter must not be a raw string; provide an object or separate parameters instead.");
                }

                // parse raw key=value&key2=val2 and re-encode values
                var pairs = raw.Split('&', StringSplitOptions.RemoveEmptyEntries);
                var reEncoded = new List<string>();
                foreach (var pair in pairs)
                {
                    var idx = pair.IndexOf('=');
                    if (idx < 0)
                    {
                        // treat as key with empty value
                        var k = WebUtility.UrlEncode(pair);
                        reEncoded.Add($"{k}=");
                    }
                    else
                    {
                        var k = pair.Substring(0, idx);
                        var v = pair.Substring(idx + 1);
                        var ke = WebUtility.UrlEncode(k);
                        var ve = WebUtility.UrlEncode(v);
                        reEncoded.Add($"{ke}={ve}");
                    }
                }

                var body = string.Join("&", reEncoded);
                return new StringContent(body, Encoding.UTF8, contentType);
            }

            foreach (var param in parameters)
            {
                var pName = param.Parameter.Name;
                var pValue = param.Value;

                foreach (var keyValuePair in commonUtils.ToObjDictionary(pName, pValue))
                {
                    // overwrite existing keys with later values (consistent behavior)
                    dict[keyValuePair.Key] = keyValuePair.Value;
                }
            }

            if (string.Equals(contentType, "application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase))
            {
                // FormUrlEncodedContent will perform proper URL encoding of keys and values
                var formDict = dict.ToDictionary(kvp => kvp.Key, kvp => kvp.Value?.ToString() ?? string.Empty);
                return new FormUrlEncodedContent(formDict);
            }

            // 默认把 dict 转成 key=val&...（对 key/value 做 URL 编码）
            var encodedPairs = dict.Select(kvp =>
            {
                var k = WebUtility.UrlEncode(kvp.Key);
                var v = WebUtility.UrlEncode(kvp.Value?.ToString() ?? string.Empty);
                return $"{k}={v}";
            });

            var joined = string.Join("&", encodedPairs);
            return new StringContent(joined, Encoding.UTF8, contentType);
        }

        // 非 asUrlencoded 的情况：字符串直接发送，否则序列化为 JSON
        if (isStringType)
        {
            return new StringContent(value?.ToString() ?? string.Empty, Encoding.UTF8, contentType);
        }

        return new StringContent(jsonSerializer.Serialize(value), Encoding.UTF8, contentType);
    }
}