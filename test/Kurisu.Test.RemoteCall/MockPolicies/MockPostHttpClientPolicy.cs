using System.Net;
using System.Text;
using Kurisu.RemoteCall.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;

namespace Kurisu.Test.RemoteCall.MockPolicies;

public class MockPostHttpClientPolicy : IRemoteCallPolicyHandler
{
    public IHttpClientBuilder ConfigureHttpClient(IHttpClientBuilder builder)
    {
        return builder.ConfigurePrimaryHttpMessageHandler(() =>
        {
            var mock = new MockHttpMessageHandler();
            mock.When(HttpMethod.Post, "http://localhost:5000/api/post1")
                .Respond(httpRequest =>
                {
                    var uri = httpRequest.RequestUri!;

                    var result = httpRequest.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                    // 4. 创建并返回 HttpResponseMessage
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(result, Encoding.UTF8, "application/json")
                    };
                });

            return mock;
        });
    }
}