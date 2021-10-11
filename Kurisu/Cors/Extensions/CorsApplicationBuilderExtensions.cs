using System;
using Kurisu.DependencyInjection.Attributes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Kurisu.Cors.Extensions
{
    /// <summary>
    /// 跨域中间件扩展
    /// </summary>
    [SkipScan]
    public static class CorsApplicationBuilderExtensions
    {
        /// <summary>
        /// 使用跨域中间件
        /// </summary>
        /// <param name="app"></param>
        /// <param name="corsPolicybuilder"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseCorsPolicy(this IApplicationBuilder app,
            Action<CorsPolicyBuilder> corsPolicybuilder = default)
        {
            var corsAppSetting = app.ApplicationServices.GetService<IOptions<CorsAppSetting>>()?.Value;

            return corsPolicybuilder == null
                ? app.UseCors(corsAppSetting?.PolicyName)
                : app.UseCors(corsPolicybuilder);
        }
    }
}