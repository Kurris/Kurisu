using Newtonsoft.Json;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using System.Net.Mime;
using System.Text;
using Kurisu.Core.Proxy;
using Kurisu.Core.Proxy.Abstractions;
using Kurisu.RemoteCall.Attributes;
using Mapster;
using Microsoft.Extensions.Logging;
using Kurisu.Core.User.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.RemoteCall.Aops;

/// <summary>
/// 默认远程调用
/// </summary>
public class DefaultRemoteCallClient : Aop
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DefaultRemoteCallClient> _logger;

    public DefaultRemoteCallClient(
        IServiceProvider serviceProvider,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<DefaultRemoteCallClient> logger)
    {
        _serviceProvider = serviceProvider;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }


    protected override async Task InterceptAsync(IProxyInvocation invocation, Func<IProxyInvocation, Task> proceed)
    {
        await RequestAsync(invocation);
    }

    protected override async Task<TResult> InterceptAsync<TResult>(IProxyInvocation invocation, Func<IProxyInvocation, Task<TResult>> proceed)
    {
        TResult result;

        var responseJson = await RequestAsync(invocation);
        var type = typeof(TResult);

        if (type.IsClass && type != typeof(string))
        {
            result = JsonConvert.DeserializeObject<TResult>(responseJson, InteralHelper.JsonSerializerSettings);
        }
        else
        {
            result = responseJson.Adapt<TResult>();
        }

        return await Task.FromResult(result);
    }

    /// <summary>
    /// 请求处理
    /// </summary>
    /// <param name="invocation"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    /// <exception cref="FileLoadException"></exception>
    private async Task<string> RequestAsync(IProxyInvocation invocation)
    {
        //参数和值
        var methodParameters = invocation.Method.GetParameters();
        var methodParameterValues = methodParameters.Select((t, i) => new KeyValuePair<ParameterInfo, object>(t, invocation.Parameters[i])).ToList();

        //请求方式和地址
        var enableRemoteClientAttribute = invocation.InterfaceType.GetCustomAttribute<EnableRemoteClientAttribute>()!;
        var httpMethodAttribute = invocation.Method.GetCustomAttribute<HttpMethodAttribute>();
        (HttpMethodEnumType? httpMethodType, string url) = InteralHelper.GetRequestUrl(_configuration, enableRemoteClientAttribute, httpMethodAttribute, methodParameterValues);

        //请求方法
        var callMethod = (httpMethodType == HttpMethodEnumType.Get
                             ? typeof(HttpClient).GetMethod(httpMethodType + "Async", new[] { typeof(string) })
                             : typeof(HttpClient).GetMethod(httpMethodType + "Async", new[] { typeof(string), typeof(HttpContent) }))
                         ?? throw new NotSupportedException($"不支持{httpMethodType}的请求方式");

        //请求方法的参数
        var requestParameters = new List<object>(2) { url };

        if (httpMethodType != HttpMethodEnumType.Get)
        {
            //找form参数(上传文件)
            var formData = methodParameterValues.FirstOrDefault(x => x.Key.IsDefined(typeof(RequestFormAttribute)));
            if (!formData.Equals(default(KeyValuePair<ParameterInfo, object>)))
            {
                var filePath = formData.Value.ToString()!;
                var content = new MultipartFormDataContent();

                if (formData.Key.ParameterType == typeof(string))
                {
                    //todo 大文件问题
                    content.Add(new ByteArrayContent(await File.ReadAllBytesAsync(filePath)), "file", Path.GetFileName(filePath));
                    //其他参数作为StringContent
                    foreach (var item in methodParameterValues.Where(x => x.Key.Name != formData.Key.Name))
                    {
                        content.Add(new StringContent(item.Value.ToString()!), item.Key.Name!);
                    }
                }
                else if (formData.Key.ParameterType == typeof(byte[]))
                {
                    var fileInfos = methodParameterValues.Where(x => x.Key.Name != formData.Key.Name).ToList();
                    if (!fileInfos.Any())
                    {
                        throw new FileNotFoundException("请在其他参数中需要明确文件流的fileName");
                    }

                    content.Add(new ByteArrayContent((byte[])formData.Value), "file", fileInfos.First().Value.ToString()!);
                    var others = methodParameterValues.Where(x => x.Key.Name != formData.Key.Name).Reverse().ToList();
                    //不处理fileName的参数
                    for (int i = 0; i < others.Count - 1; i++)
                    {
                        var item = others.ElementAt(i);
                        content.Add(new StringContent(item.Value.ToString()!), item.Key.Name!);
                    }
                }
                else if (formData.Key.ParameterType == typeof(Stream))
                {
                    throw new NotImplementedException();
                }

                requestParameters.Add(content);
            }
            else
            {
                var body = "{}";

                //MediaTypeAttribute
                string mediaType = invocation.Method.GetCustomAttribute<RequestMediaTypeAttribute>()?.Type ?? MediaTypeNames.Application.Json;
                if (mediaType == MediaTypeNames.Application.Json)
                {
                    if (methodParameterValues.Any() && methodParameterValues[0].Key.ParameterType != typeof(string))
                    {
                        body = JsonConvert.SerializeObject(methodParameterValues[0].Value, InteralHelper.JsonSerializerSettings);
                    }
                }
                // else if (mediaType == "application/x-www-form-urlencoded")
                // {
                //     
                // }

                var content = new StringContent(body, Encoding.UTF8, mediaType);
                //添加发送内容
                requestParameters.Add(content);
            }
        }

        var httpClient = string.IsNullOrEmpty(enableRemoteClientAttribute.Name)
            ? _httpClientFactory.CreateClient()
            : _httpClientFactory.CreateClient(enableRemoteClientAttribute.Name);

        if (requestParameters.Count > 1)
        {
            _logger.LogInformation("{Method} {Url} \r\n Body:{Body} .", httpMethodType, url, JsonConvert.SerializeObject(requestParameters.LastOrDefault()));
        }
        else
        {
            _logger.LogInformation("{Method} {Url} .", httpMethodType, url);
        }

        //鉴权判断
        if (InteralHelper.UseAuth(invocation))
        {
            var user = _serviceProvider.GetService<ICurrentUser>();
            var token = user.GetToken();
            httpClient.DefaultRequestHeaders.Add("Authorization", token);
        }

        var task = (Task<HttpResponseMessage>)callMethod.Invoke(httpClient, requestParameters.ToArray())!;
        var response = await task.ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var responseJson = await response.Content.ReadAsStringAsync();
        return responseJson;
    }
}