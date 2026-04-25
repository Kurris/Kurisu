using System.Net;
using System.Text;
using Kurisu.RemoteCall.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;

namespace Kurisu.Test.RemoteCall.MockPolicies;

public class MockCustomContentPolicy : IRemoteCallPolicyHandler
{
    public IHttpClientBuilder ConfigureHttpClient(IHttpClientBuilder builder)
    {
        return builder.ConfigurePrimaryHttpMessageHandler(() =>
        {
            var mock = new MockHttpMessageHandler();

            mock.When(HttpMethod.Post, "http://localhost:5006/api/custom/upload")
                .Respond(async req =>
                {
                    var body = await req.Content.ReadAsStringAsync();
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(body, Encoding.UTF8, "application/json")
                    };
                });

            return mock;
        });
    }
}

