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
                //假定value 就是 name=ligy&age=18
                return new StringContent(value.ToString()!, Encoding.UTF8, postAttribute.ContentType);
            }

            if (postAttribute.ContentType == "application/x-www-form-urlencoded")
            {
                var dict = value.ToStringDictionary();
                return new FormUrlEncodedContent(dict);
            }

            //application/json --> name=ligy&age=18
            var body = string.Join("&", value.ToStringDictionary().Select(x => $"{x.Key}={x.Value}"));
            return new StringContent(body, Encoding.UTF8, postAttribute.ContentType);
        }

        if (isStringType)
        {
            return new StringContent(value.ToString()!, Encoding.UTF8, postAttribute.ContentType);
        }

        return new StringContent(JsonConvert.SerializeObject(value), Encoding.UTF8, postAttribute.ContentType);
    }
}