using Kurisu.Core.Proxy.Abstractions;
using Kurisu.RemoteCall.Attributes;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Reflection;

namespace Kurisu.RemoteCall;

public static class InteralHelper
{
    public static JsonSerializerSettings JsonSerializerSettings { get; set; } = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
    };


    /// <summary>
    /// 获取生成的请求url
    /// </summary>
    /// <param name="setting"></param>
    /// <param name="methodTemplate"></param>
    /// <param name="parameters"></param>
    /// <returns></returns>
    public static (HttpMethodEnumType? httpMethodType, string url) GetRequestUrl(IConfiguration configuration,
        EnableRemoteClientAttribute setting,
        HttpMethodAttribute methodTemplate, List<KeyValuePair<ParameterInfo, object>> parameters)
    {
        var url = methodTemplate?.Template ?? string.Empty;
        setting.BaseUrl = FixConfiguration(configuration, setting.BaseUrl);
        url = FixConfiguration(configuration, url);
        url = FixFromQuery(url, parameters);

        var httpMethod = methodTemplate?.HttpMethod;
        return string.IsNullOrEmpty(setting.BaseUrl) || url.StartsWith("http")
            ? (httpMethod, url)
            : (httpMethod, setting.BaseUrl?.TrimEnd('/') + (url.StartsWith('/') ? url : "/" + url));
    }

    /// <summary>
    /// 处理url地址
    /// </summary>
    /// <param name="url"></param>
    /// <param name="parameters"></param>
    /// <returns></returns>
    private static string FixFromQuery(string url, List<KeyValuePair<ParameterInfo, object>> parameters)
    {
        //表单提交
        if (parameters.Any(x => x.Key.IsDefined(typeof(RequestFormAttribute))))
        {
            return url;
        }

        //处理请求url的地址
        List<string> items = new(parameters.Count);
        var p = parameters.FirstOrDefault(x => x.Key.IsDefined(typeof(RequestQueryAttribute)) && x.Key.ParameterType != typeof(string));
        if (!p.Equals(default(KeyValuePair<ParameterInfo, object>)))
        {
            //对象转字典
            var d = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(p.Value, JsonSerializerSettings))!;
            items.AddRange(d.Select(pair => $"{pair.Key}={pair.Value}"));

            //过滤当前的参数
            parameters = parameters.Where(x => x.Key != p.Key).ToList();
        }

        foreach (var item in parameters)
        {
            if (url.Contains($"{{{item.Key.Name}}}"))
            {
                url = url.Replace($"{{{item.Key.Name}}}", item.Value.ToString());
            }
            else
            {
                items.Add($"{item.Key.Name}={item.Value}");
            }
        }

        return items.Any()
              ? url + "?" + string.Join("&", items)
              : url;
    }


    /// <summary>
    /// 从配置中转换
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    public static string FixConfiguration(IConfiguration configuration, string url)
    {
        if (!url.StartsWith("${") || !url.EndsWith("}"))
        {
            return url;
        }

        var name = url.Replace("${", "").TrimEnd('}');
        url = configuration.GetSection(name).Value;

        return url;
    }



    /// <summary>
    /// 判断请求使用使用授权
    /// </summary>
    /// <param name="invocation"></param>
    /// <returns></returns>
    public static bool UseAuth(IProxyInvocation invocation)
    {
        return invocation.InterfaceType.GetCustomAttribute<AuthAttribute>() != null || invocation.Method.GetCustomAttribute<AuthAttribute>() != null;
    }
}
