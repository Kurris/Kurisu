using System.Net;
using System.Text;
using Kurisu.RemoteCall.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;

namespace Kurisu.Test.RemoteCall.MockPolicies;

public class MockAuthPolicy : IRemoteCallPolicyHandler
{
    public IHttpClientBuilder ConfigureHttpClient(IHttpClientBuilder builder)
    {
        return builder.ConfigurePrimaryHttpMessageHandler(() =>
        {
            var mock = new MockHttpMessageHandler();

            mock.When(HttpMethod.Get, "http://localhost:5003/api/auth/check")
                .Respond(req =>
                {
                    var auth = req.Headers.Authorization?.ToString() ?? string.Empty;
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(new { auth }.ToJson(), Encoding.UTF8, "application/json")
                    };
                });

            return mock;
        });
    }
}
