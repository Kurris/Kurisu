using System;
using Kurisu.ConfigurableOptions.Attributes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Kurisu.ConfigurableOptions.Abstractions
{
    /// <summary>
    /// 后置配置
    /// </summary>
    /// <typeparam name="TOptions">配置类型</typeparam>
    public interface IPostConfigure<in TOptions> where TOptions : class, new()
    {
        void PostConfigure(IConfiguration configuration, TOptions options);
    }
}