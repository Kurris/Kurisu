using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.AspNetCore.Utils.Extensions;

public static class HttpClientBuilderExtensions
{
    /// <summary>
    /// 配置 HttpClient 跳过 SSL 证书校验。
    /// </summary>
    /// <param name="builder">HttpClient 构建器。</param>
    /// <returns>配置后的构建器。</returns>
    public static IHttpClientBuilder NotVerifyCertificate(this IHttpClientBuilder builder)
    {
        return builder.ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new HttpClientHandler
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            };

            return handler;
        });
    }
}