using Kurisu.RemoteCall.Abstractions;
using Kurisu.RemoteCall.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.RemoteCall.Attributes;

/// <summary>
/// 启用远程调用
/// </summary>
[AttributeUsage(AttributeTargets.Interface)]
public sealed class EnableRemoteClientAttribute : Attribute
{
    /// <summary>
    /// 启用远程调用
    /// </summary>
    /// <param name="baseUrl">远程地址</param>
    public EnableRemoteClientAttribute(string baseUrl) : this(string.Empty, baseUrl)
    {
    }

    /// <summary>
    /// 启用远程调用
    /// </summary>
    /// <param name="name">命名远程资源</param>
    /// <param name="baseUrl">远程地址</param>
    public EnableRemoteClientAttribute(string name, string baseUrl)
    {
        if (!string.IsNullOrEmpty(baseUrl) && baseUrl.EndsWith("/"))
        {
            throw new ArgumentException("baseUrl不可以 / 结尾");
        }

        Name = name;
        BaseUrl = baseUrl;
    }

    /// <summary>
    /// client name
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// BaseUrl,支持从<see cref="IConfiguration"/>中获取,如$(Path)
    /// </summary>
    public string BaseUrl { get; }

    /// <summary>
    /// 请求处理策略<see cref="IRemoteCallPolicyHandler"/>
    /// </summary>
    public Type PolicyHandler { get; set; }

    /// <summary>
    /// 配置服务
    /// </summary>
    /// <param name="services"></param>
    public void ConfigureServices(IServiceCollection services)
    {
        if (HandlerCache.ConfigClients.Contains(Name))
        {
            throw new Exception("重复的服务命名:" + Name);
        }

        if (string.IsNullOrEmpty(Name))
        {
            if (PolicyHandler != null)
            {
                throw new ArgumentException("当PolicyHandler不为空时," + nameof(Name) + "不能为空");
            }

            services.AddHttpClient();
        }
        else
        {
            var builder = services.AddHttpClient(Name);
            if (PolicyHandler.IsImplementFrom<IRemoteCallPolicyHandler>())
            {
                ((IRemoteCallPolicyHandler)Activator.CreateInstance(PolicyHandler))!.ConfigureHttpClient(builder);
            }
        }

        HandlerCache.ConfigClients.Add(Name);
    }
}