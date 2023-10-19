using HttpMethod = Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http.HttpMethod;


using System;
using System.Net.Http;
using System.Threading.Tasks;
using Kurisu.Proxy.Abstractions;
using Kurisu.Proxy;
using Newtonsoft.Json;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Mapster;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Routing;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Serialization;
using System.Net.Mime;
using System.Text;
using Kurisu.RemoteCall.Atributes;
using Kurisu.RemoteCall.Extensions;
using Microsoft.Extensions.Logging;
using System.IO;

namespace Kurisu.RemoteCall;

[SkipScan]
internal class DefaultRemoteCallClient : Aop
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DefaultRemoteCallClient> _logger;
    private readonly JsonSerializerSettings _jsonSerializerSettings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
    };

    public DefaultRemoteCallClient(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<DefaultRemoteCallClient> logger)
    {
        _httpClient = httpClientFactory.CreateClient(EnableRemoteClientExtensions.HttpClientName);
        _configuration = configuration;
        _logger = logger;
    }


    protected override async Task InterceptAsync(IProxyInvocation invocation, Func<IProxyInvocation, Task> proceed)
    {
        await ReuqestAsync(invocation);
    }

    protected override async Task<TResult> InterceptAsync<TResult>(IProxyInvocation invocation, Func<IProxyInvocation, Task<TResult>> proceed)
    {
        TResult result;

        var responseJson = await ReuqestAsync(invocation);

        if (typeof(TResult).IsClass && typeof(TResult) != typeof(string))
        {
            result = JsonConvert.DeserializeObject<TResult>(responseJson, _jsonSerializerSettings);
        }
        else
        {
            result = responseJson.Adapt<TResult>();

        }

        return await Task.FromResult(result);
    }

    private async Task<string> ReuqestAsync(IProxyInvocation invocation)
    {
        //参数和值
        var ps = invocation.Method.GetParameters();
        var p = new List<KeyValuePair<ParameterInfo, object>>();
        for (int i = 0; i < ps.Length; i++)
            p.Add(new KeyValuePair<ParameterInfo, object>(ps[i], invocation.Parameters[i]));

        //请求方式和地址
        var enableRemoteClientAttribute = invocation.InterfaceType.GetCustomAttribute<EnableRemoteClientAttribute>();
        var httpMethodAttribute = invocation.Method.GetCustomAttribute<HttpMethodAttribute>();
        var (httpMethod, url) = GetRequestUrl(enableRemoteClientAttribute, httpMethodAttribute, p);
        //请求方法
        var callMethod = (httpMethod == HttpMethod.Get
            ? typeof(HttpClient).GetMethod(httpMethod.ToString() + "Async", new Type[] { typeof(string) })
            : typeof(HttpClient).GetMethod(httpMethod.ToString() + "Async", new Type[] { typeof(string), typeof(HttpContent) }))
            ?? throw new NotSupportedException("不支持" + httpMethod.ToString() + "的请求方式");

        //请求方法的参数
        var requestObjects = new List<object> { url };

        if (httpMethod != HttpMethod.Get)
        {
            var formData = p.FirstOrDefault(x => x.Key.IsDefined(typeof(FromFormAttribute)));
            if (!formData.Equals(default(KeyValuePair<ParameterInfo, object>)))
            {
                var filePath = formData.Value.ToString();
                var content = new MultipartFormDataContent();
                if (formData.Key.ParameterType == typeof(string))
                {
                    content.Add(new ByteArrayContent(await File.ReadAllBytesAsync(filePath)), "file", Path.GetFileName(filePath));
                    foreach (var item in p.Where(x => x.Key.Name != formData.Key.Name))
                    {
                        content.Add(new StringContent(item.Value.ToString()), item.Key.Name);
                    }
                }
                else if (formData.Key.ParameterType == typeof(byte[]))
                {
                    var fileInfos = p.Where(x => x.Key.Name != formData.Key.Name);
                    if (fileInfos.Count() < 1)
                    {
                        throw new FileLoadException("请在其他参数中需要明确文件流的fileName");
                    }
                    content.Add(new ByteArrayContent((byte[])formData.Value), "file", fileInfos.ElementAt(0).Value.ToString());
                    var others = p.Where(x => x.Key.Name != formData.Key.Name).Reverse();
                    for (int i = 0; i < others.Count() - 1; i++)
                    {
                        var item = others.ElementAt(i);
                        content.Add(new StringContent(item.Value.ToString()), item.Key.Name);
                    }
                }

                requestObjects.Add(content);
            }
            else
            {
                //MediaTypeAttribute
                string mediaType = invocation.Method.GetCustomAttribute<MediaTypeAttribute>()?.Type ?? MediaTypeNames.Application.Json;
                var body = mediaType == MediaTypeNames.Application.Json
                       ? JsonConvert.SerializeObject(invocation.Parameters[0], _jsonSerializerSettings)
                       : invocation.Parameters[0].ToString();

                var content = new StringContent(body, Encoding.UTF8, mediaType);

                //添加发送内容
                requestObjects.Add(content);
            }
        }

        //_logger.LogInformation("请求参数:{Json}", JsonConvert.SerializeObject(requestObjects));

        var task = (Task<HttpResponseMessage>)callMethod.Invoke(_httpClient, requestObjects.ToArray());
        var response = await task.ConfigureAwait(false);
        var responseJson = await response.Content.ReadAsStringAsync();
        return responseJson;
    }


    private (HttpMethod httpMethod, string url) GetRequestUrl(EnableRemoteClientAttribute setting, HttpMethodAttribute methodTemplate, List<KeyValuePair<ParameterInfo, object>> parameters)
    {
        var url = methodTemplate?.Template ?? string.Empty;
        url = FixConfiguration(url);
        url = FixFromQuery(url, parameters);

        var httpMethod = Enum.Parse<HttpMethod>(methodTemplate?.HttpMethods?.ElementAt(0) ?? "None", true);
        if (string.IsNullOrEmpty(setting.BaseUrl))
        {
            return (httpMethod, url);
        }

        return (httpMethod, setting.BaseUrl?.TrimEnd('/') + (url.StartsWith('/') ? url : "/" + url));
    }

    private string FixFromQuery(string url, List<KeyValuePair<ParameterInfo, object>> parameters)
    {
        if (parameters.Any(x => x.Key.IsDefined(typeof(FromFormAttribute))))
        {
            return url;
        }

        var p = parameters.FirstOrDefault(x => x.Key.IsDefined(typeof(FromQueryAttribute)) && x.Key.ParameterType != typeof(string));
        if (!p.Equals(default(KeyValuePair<ParameterInfo, object>)))
        {
            var d = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(p.Value, _jsonSerializerSettings));
            return url + "?" + string.Join("&", d.Select(x => x.Key + "=" + x.Value));
        }
        List<string> strings = new(parameters.Count);
        foreach (var item in parameters)
        {
            if (url.Contains($"{{{item.Key.Name}}}"))
            {
                url = url.Replace($"{{{item.Key.Name}}}", item.Value.ToString());
            }
            else
            {
                strings.Add($"{item.Key.Name}={item.Value}");
            }
        }

        if (parameters.Any() && parameters[0].Key.ParameterType.IsClass && parameters[0].Key.ParameterType != typeof(string))
        {
            return url;
        }

        return url + "?" + string.Join("&", strings);
    }

    private string FixConfiguration(string url)
    {
        if (url.StartsWith("${") && url.EndsWith("}"))
        {
            url = _configuration.GetValue<string>(url.Replace("${", "").TrimEnd('}'));
        }
        return url;
    }
}
