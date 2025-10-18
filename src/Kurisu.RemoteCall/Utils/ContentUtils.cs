using System.Net.Mime;
using System.Reflection;
using System.Text;
using Kurisu.RemoteCall.Abstractions;
using Kurisu.RemoteCall.Attributes;
using Newtonsoft.Json;

namespace Kurisu.RemoteCall.Utils;

internal class ContentUtils
{
    public static HttpContent Create(MethodInfo method, List<ParameterValue> parameters)
    {
        var body = "{}";

        //MediaTypeAttribute
        var requestMediaType = method.GetCustomAttribute<RequestMediaTypeAttribute>();
        var mediaType = requestMediaType?.ContentType ?? MediaTypeNames.Application.Json;
        var isUrlEncoded = requestMediaType?.IsUrlEncoded ?? false;

        if (isUrlEncoded)
        {
            if (mediaType == "application/x-www-form-urlencoded")
            {
                //实体参数
                if (parameters.Any() && parameters[0].Parameter.ParameterType != typeof(string))
                {
                    var d = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(parameters[0].Value))!;
                    body = string.Join("&", d.Select(pair => $"{pair.Key}={pair.Value}"));
                }
            }
            else
            {
                if (parameters.Any() && parameters[0].Parameter.ParameterType != typeof(string))
                {
                    var d = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(parameters[0].Value))!;
                    return new FormUrlEncodedContent(d);
                }
            }
        }
        else
        {
            if (parameters.Any())
            {
                var currentType = parameters[0].Parameter.ParameterType;
                if (currentType != typeof(string))
                {
                    body = JsonConvert.SerializeObject(parameters[0].Value);
                }
            }
        }

        return new StringContent(body, Encoding.UTF8, mediaType);
    }
}