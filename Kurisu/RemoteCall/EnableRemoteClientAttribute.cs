using System;
using System.ComponentModel;
using System.Net.Http;
using System.Threading.Tasks;
using Kurisu.Proxy.Abstractions;
using Kurisu.Proxy;
using Kurisu.Proxy.Attributes;
using Newtonsoft.Json;
using System.Reflection;
using Kurisu.UnifyResultAndValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.RemoteCall
{
    public class EnableRemoteClientAttribute : AopAttribute
    {
        public EnableRemoteClientAttribute() : base(typeof(EnableHttpClient))
        {
        }

        public string Name { get; set; }

        public string BaseUrl { get; set; }

        public Type FallbackType { get; set; }
    }


    [SkipScan]
    public class EnableHttpClient : Aop
    {
        protected override async Task InterceptAsync(IProxyInfo invocation, Func<IProxyInfo, Task> proceed)
        {
            var t = invocation.Method.GetCustomAttribute<DescriptionAttribute>();
            var httpClient = new HttpClient();
            var str = "";//await httpClient.GetStringAsync(t.Description);

            await proceed(invocation).ConfigureAwait(false);
        }

        protected override async Task<TResult> InterceptAsync<TResult>(IProxyInfo invocation, Func<IProxyInfo, Task<TResult>> proceed)
        {
            var t = invocation.Method.GetCustomAttribute<DescriptionAttribute>().Description;
            //var httpClient = new HttpClient();
            var v1 = JsonConvert.SerializeObject(new DefaultApiResult<object> { State = ApiStateCode.Success });
            return await Task.FromResult(JsonConvert.DeserializeObject<TResult>(v1));
        }
    }
}
