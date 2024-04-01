using Kurisu.Core.Proxy.Abstractions;
using Kurisu.Core.User.Abstractions;
using Kurisu.RemoteCall.Abstractions;
using Kurisu.RemoteCall.Attributes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Net.Mime;
using System.Reflection;
using System.Text;

namespace Kurisu.RemoteCall;

internal static class InteralHelper
{
    public static JsonSerializerSettings JsonSerializerSettings { get; set; } = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
    };

    /// <summary>
    /// 获取生成的请求url
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="setting"></param>
    /// <param name="methodTemplate"></param>
    /// <param name="parameters"></param>
    /// <returns></returns>
    public static (HttpMethodEnumType? httpMethodType, string url) GetRequestUrl(IConfiguration configuration,
        EnableRemoteClientAttribute setting,
        HttpMethodAttribute methodTemplate,
        List<KeyValuePair<ParameterInfo, object>> parameters)
    {
        var url = methodTemplate?.Template ?? string.Empty;
        var baseUrl = setting.BaseUrl ?? string.Empty;

        baseUrl = FixFromConfiguration(configuration, baseUrl);
        url = FixFromConfiguration(configuration, url);
        url = FixQuery(url, parameters);

        var httpMethod = methodTemplate?.HttpMethod;
        return string.IsNullOrEmpty(baseUrl) || url.StartsWith("http")
            ? (httpMethod, url)
            : (httpMethod, baseUrl.TrimEnd('/') + (url.StartsWith('/') ? url : "/" + url));
    }

    /// <summary>
    /// 处理url地址
    /// </summary>
    /// <param name="url"></param>
    /// <param name="parameters"></param>
    /// <returns></returns>
    private static string FixQuery(string url, List<KeyValuePair<ParameterInfo, object>> parameters)
    {
        //表单提交
        if (parameters.Any(x => x.Key.IsDefined(typeof(RequestFormAttribute))))
            return url;

        //处理请求url的地址
        List<string> items = new(parameters.Count);

        //存在定义RequestQueryAttribute
        var p = parameters.FirstOrDefault(x => x.Key.IsDefined(typeof(RequestQueryAttribute)) && x.Key.ParameterType != typeof(string));
        if (!p.Equals(default(KeyValuePair<ParameterInfo, object>)))
        {
            //对象转字典
            var d = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(p.Value, JsonSerializerSettings))!;
            items.AddRange(d.Select(pair => $"{pair.Key}={pair.Value}"));
        }
        else
        {
            foreach (var item in parameters)
            {
                if (url.Contains($"{{{item.Key.Name}}}"))
                {
                    url = url.Replace($"{{{item.Key.Name}}}", item.Value.ToString());
                }
                else
                {
                    if (!item.Key.ParameterType.IsClass || item.Key.ParameterType == typeof(string))
                    {
                        items.Add($"{item.Key.Name}={item.Value}");
                    }
                }
            }
        }

        return items.Any()
              ? url + "?" + string.Join("&", items)
              : url;
    }


    /// <summary>
    /// 从配置中转换
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="template"></param>
    /// <returns></returns>
    public static string FixFromConfiguration(IConfiguration configuration, string template)
    {
        if (!template.StartsWith("${") || !template.EndsWith("}"))
        {
            return template;
        }

        var path = template.Replace("${", "").TrimEnd('}');
        return configuration.GetSection(path).Value;
    }

    /// <summary>
    /// 判断请求使用授权
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="invocation"></param>
    /// <param name="headerName"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public static bool UseAuth(IServiceProvider serviceProvider, IProxyInvocation invocation, out string headerName, out string token)
    {
        headerName = string.Empty;
        token = string.Empty;

        var authAttribute = invocation.Method.GetCustomAttribute<AuthAttribute>() ?? invocation.InterfaceType.GetCustomAttribute<AuthAttribute>();
        if (authAttribute != null)
        {
            headerName = authAttribute.HeaderName;
            if (authAttribute.TokenHandler != null && authAttribute.TokenHandler.IsAssignableTo(typeof(IAuthTokenHandler)))
            {
                token = ((IAuthTokenHandler)Activator.CreateInstance(authAttribute.TokenHandler))!.GetToken(serviceProvider);
                return true;
            }
            else
            {
                var user = serviceProvider.GetService<ICurrentUser>();
                token = user.GetToken();
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 判断请求输出日志
    /// </summary>
    /// <param name="invocation"></param>
    /// <returns></returns>
    public static bool UseLog(IProxyInvocation invocation)
    {
        return (invocation.InterfaceType.GetCustomAttribute<RequestLogAttribute>() ?? invocation.Method.GetCustomAttribute<RequestLogAttribute>()) != null;
    }


    public static async Task<HttpContent> FixContentAsync(IProxyInvocation invocation, List<KeyValuePair<ParameterInfo, object>> parameters)
    {
        //找form参数(上传文件)
        var formData = parameters.FirstOrDefault(x => x.Key.IsDefined(typeof(RequestFormAttribute)));
        if (!formData.Equals(default(KeyValuePair<ParameterInfo, object>)))
        {
            var content = new MultipartFormDataContent();

            //路径上传文件
            if (formData.Key.ParameterType == typeof(string))
            {
                var filePath = formData.Value.ToString()!;
                //todo 大文件问题
                content.Add(new ByteArrayContent(await File.ReadAllBytesAsync(filePath)), "file", Path.GetFileName(filePath));
                //其他参数作为StringContent
                foreach (var item in parameters.Where(x => x.Key.Name != formData.Key.Name))
                {
                    content.Add(new StringContent(item.Value.ToString()!), item.Key.Name!);
                }
            }
            //二进制上传
            else if (formData.Key.ParameterType == typeof(byte[]))
            {
                var fileInfos = parameters.Where(x => x.Key.Name != formData.Key.Name).ToList();
                if (!fileInfos.Any())
                {
                    throw new FileNotFoundException("请在其他参数中明确fileName");
                }

                content.Add(new ByteArrayContent((byte[])formData.Value), "file", fileInfos.First().Value.ToString()!);
                var others = parameters.Where(x => x.Key.Name != formData.Key.Name).Reverse().ToList();
                //不处理fileName的参数
                for (int i = 0; i < others.Count - 1; i++)
                {
                    var item = others.ElementAt(i);
                    content.Add(new StringContent(item.Value.ToString()!), item.Key.Name!);
                }
            }
            //流上传
            else if (formData.Key.ParameterType == typeof(Stream))
            {
                var fileInfos = parameters.Where(x => x.Key.Name != formData.Key.Name).ToList();
                if (!fileInfos.Any())
                {
                    throw new FileNotFoundException("请在其他参数中明确fileName");
                }

                content.Add(new StreamContent((Stream)formData.Value), "file", fileInfos.First().Value.ToString()!);
                var others = parameters.Where(x => x.Key.Name != formData.Key.Name).Reverse().ToList();
                //不处理fileName的参数
                for (int i = 0; i < others.Count - 1; i++)
                {
                    var item = others.ElementAt(i);
                    content.Add(new StringContent(item.Value.ToString()!), item.Key.Name!);
                }
            }

            return content;
        }
        else
        {
            var body = "{}";

            //MediaTypeAttribute
            string mediaType = invocation.Method.GetCustomAttribute<RequestMediaTypeAttribute>()?.ContentType ?? MediaTypeNames.Application.Json;
            if (mediaType == MediaTypeNames.Application.Json)
            {
                if (parameters.Any() && parameters[0].Key.ParameterType != typeof(string))
                {
                    body = JsonConvert.SerializeObject(parameters[0].Value, JsonSerializerSettings);
                }
            }
            else if (mediaType == "application/x-www-form-urlencoded")
            {
                if (parameters.Any() && parameters[0].Key.ParameterType != typeof(string))
                {
                    var d = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(parameters[0].Value, JsonSerializerSettings))!;
                    body = string.Join("&", d.Select(pair => $"{pair.Key}={pair.Value}"));
                }
            }

            var content = new StringContent(body, Encoding.UTF8, mediaType);
            return content;
        }
    }
}
