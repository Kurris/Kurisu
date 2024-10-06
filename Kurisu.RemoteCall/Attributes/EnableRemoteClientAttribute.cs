using Kurisu.RemoteCall.Abstractions;
using Kurisu.RemoteCall.Default;
using Kurisu.RemoteCall.Proxy.Attributes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.RemoteCall.Attributes;

/// <summary>
/// 启用远程调用
/// </summary>
[AttributeUsage(AttributeTargets.Interface)]
public sealed class EnableRemoteClientAttribute : AopAttribute
{
    /// <summary>
    /// ctor
    /// </summary>
    public EnableRemoteClientAttribute() : base(typeof(DefaultRemoteCallClient))
    {
    }

    /// <summary>
    /// client name
    /// </summary>
    /// <remarks>
    /// 如果为空,使用default-client
    /// </remarks>
    public string Name { get; set; }

    /// <summary>
    /// BaseUrl,支持从<see cref="IConfiguration"/>中获取,如$(Path)
    /// </summary>
    public string BaseUrl { get; set; }

    /// <summary>
    /// 请求处理策略<see cref="IHttpRemoteCallPolicyHandler"/>
    /// </summary>
    public Type PolicyHandler { get; set; }


    /// <inheritdoc/>
    public override void ConfigureServices(IServiceCollection services)
    {
        //命名client
        if (string.IsNullOrEmpty(Name))
        {
            Name = "default-client";
        }

        var builder = services.AddHttpClient(Name);
        if (PolicyHandler != null && PolicyHandler.IsAssignableTo(typeof(IHttpRemoteCallPolicyHandler)))
        {
            ((IHttpRemoteCallPolicyHandler)Activator.CreateInstance(PolicyHandler))!.ConfigureHttpClient(builder);
        }
    }
}