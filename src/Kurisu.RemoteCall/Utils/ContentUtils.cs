using System.Net.Mime;
using System.Reflection;
using System.Text;
using Kurisu.RemoteCall.Abstractions;
using Kurisu.RemoteCall.Attributes;
using Newtonsoft.Json;

namespace Kurisu.RemoteCall.Utils;

internal class ContentUtils : IRemoteCallContentHandler
{
    public HttpContent Create(ContentInfo contentInfo)
    {
        var parameters = contentInfo.Values;

        //找form参数(上传文件)
        var formData = parameters.FirstOrDefault(x => x.Parameter.IsDefined(typeof(RequestFormAttribute)));
        if (formData != null)
        {
            var content = new MultipartFormDataContent();
            var parameter = formData.Parameter;
            var instance = formData.Value;
            var otherParameters = parameters.Where(x => x.Parameter.Name != parameter.Name).ToList();
            if (!otherParameters.Any())
                throw new FileNotFoundException("请在其他参数中明确fileName");

            if (parameter.ParameterType == typeof(byte[]))
            {
                content.Add(new ByteArrayContent((byte[])instance), "file", otherParameters.First().Value.ToString()!);
            }
            //流上传
            else if (parameter.ParameterType == typeof(Stream))
            {
                content.Add(new StreamContent((Stream)instance), "file", otherParameters.First().Value.ToString()!);
            }

            var others = otherParameters.ToList();
            others.Reverse();
            //不处理fileName的参数
            for (int i = 0; i < others.Count - 1; i++)
            {
                var item = others.ElementAt(i);
                content.Add(new StringContent(item.Value.ToString()!), item.Parameter.Name!);
            }

            return content;
        }

        var body = "{}";

        //MediaTypeAttribute
        var requestMediaType = contentInfo.Method.GetCustomAttribute<RequestMediaTypeAttribute>();
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