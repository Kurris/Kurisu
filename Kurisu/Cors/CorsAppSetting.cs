using System;
using System.ComponentModel.DataAnnotations;
using Kurisu.ConfigurableOptions.Abstractions;
using Kurisu.ConfigurableOptions.Attributes;
using Kurisu.DependencyInjection.Attributes;
using Microsoft.Extensions.Configuration;

namespace Kurisu.Cors
{
    /// <summary>
    /// 跨域策略应用配置
    /// </summary>
    [Configuration]
    public class CorsAppSetting : IPostConfigure<CorsAppSetting>
    {
        /// <summary>
        /// 策略名称
        /// </summary>
        [Required(ErrorMessage = "跨域策略名称不能为空")]
        public string PolicyName { get; set; }

        /// <summary>
        /// 允许来源域名，没有配置则允许所有来源
        /// </summary>
        public string[] WithOrigins { get; set; }

        /// <summary>
        /// 跨域请求中的凭据
        /// </summary>
        public bool? AllowCredentials { get; set; }

        /// <summary>
        /// 设置预检过期时间
        /// </summary>
        public int? SetPreflightMaxAge { get; set; }

        /// <summary>
        /// 后期配置
        /// </summary>
        /// <param name="configuration"></param>
        /// <param name="options"></param>
        public void PostConfigure(IConfiguration configuration, CorsAppSetting options)
        {
            PolicyName ??= "App.Cors.Policy";
            WithOrigins ??= Array.Empty<string>();
            AllowCredentials ??= true;
        }
    }
}