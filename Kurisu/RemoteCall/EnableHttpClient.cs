using System;
using System.ComponentModel;
using System.Net.Http;
using System.Threading.Tasks;
using Kurisu.Proxy.Abstractions;
using Kurisu.Proxy;
using Newtonsoft.Json;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Mapster;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Collections.Generic;

namespace Kurisu.RemoteCall;

[SkipScan]
internal class EnableHttpClient : Aop
{
    private readonly HttpClient _httpClient;
    public EnableHttpClient(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("kurisu.remote.httpClient");
    }


    protected override async Task InterceptAsync(IProxyInvocation invocation, Func<IProxyInvocation, Task> proceed)
    {
        var t = invocation.Method.GetCustomAttribute<DescriptionAttribute>();
        var httpClient = new HttpClient();
        var str = "";//await httpClient.GetStringAsync(t.Description);

        await proceed(invocation).ConfigureAwait(false);
    }

    protected override async Task<TResult> InterceptAsync<TResult>(IProxyInvocation invocation, Func<IProxyInvocation, Task<TResult>> proceed)
    {
        var template = invocation.Method.GetCustomAttribute<HttpGetAttribute>().Template;
        var ps = invocation.Method.GetParameters();
        var dic = new Dictionary<string, string>();
        for (int i = 0; i < ps.Length; i++)
        {
            dic.Add(ps[i].Name, invocation.Parameters[i]?.ToString());

        }

        TResult result;
        using (var content = new FormUrlEncodedContent(dic))
        {
            template = template.TrimEnd('\\') + "?" + await content.ReadAsStringAsync();
            var responseJson = await _httpClient.GetStringAsync(template);
            try
            {
                result = JsonConvert.DeserializeObject<TResult>(responseJson);
            }
            catch (Exception)
            {
                result = responseJson.Adapt<TResult>();
            }
        }

        return await Task.FromResult(result);
    }
}
