using System.Net;
using System.Text;
using Kurisu.RemoteCall.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;

namespace Kurisu.Test.RemoteCall.MockPolicies;

public class MockResponsePolicy : IRemoteCallPolicyHandler
{
    public IHttpClientBuilder ConfigureHttpClient(IHttpClientBuilder builder)
    {
        return builder.ConfigurePrimaryHttpMessageHandler(() =>
        {
            var mock = new MockHttpMessageHandler();

            mock.When(HttpMethod.Get, "http://localhost:5005/api/resp/value")
                .Respond("application/json", "{ \"code\":200, \"msg\":\"ok\", \"data\": { \"value\": \"hello\" } }");

            return mock;
        });
    }
}
