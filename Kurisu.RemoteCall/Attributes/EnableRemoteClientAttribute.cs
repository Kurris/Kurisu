using Kurisu.Core.Proxy.Attributes;
using Kurisu.RemoteCall.Abstractions;
using Kurisu.RemoteCall.Aops;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.RemoteCall.Attributes;

/// <summary>
/// 启用远程调用
/// </summary>
[AttributeUsage(AttributeTargets.Interface)]
public sealed class EnableRemoteClientAttribute : AopAttribute
{
    public EnableRemoteClientAttribute()
    {
        if (Interceptors?.Any() != true)
        {
            Interceptors = new[] { typeof(DefaultRemoteCallClient) };
        }
    }

    /// <summary>
    /// client name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// base url
    /// </summary>
    public string BaseUrl { get; set; }

    /// <summary>
    /// 请求处理策略<see cref="IHttpRemoteCallHandlerPolicy"/>
    /// </summary>
    public Type HandlerPolicy { get; set; }

    public override void ConfigureServices(IServiceCollection services)
    {
        //默认HttpClient
        services.AddHttpClient();

        //命名client
        if (string.IsNullOrEmpty(Name))
        {
            return;
        }

        var builder = services.AddHttpClient(Name);
        if (HandlerPolicy != null && HandlerPolicy.IsAssignableTo(typeof(IHttpRemoteCallHandlerPolicy)))
        {
            ((IHttpRemoteCallHandlerPolicy)Activator.CreateInstance(HandlerPolicy))!.ConfigureHttpClient(builder);
        }
    }
}