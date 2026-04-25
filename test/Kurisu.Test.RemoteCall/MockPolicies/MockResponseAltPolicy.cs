// filepath: e:\github\Kurisu\test\Kurisu.Test.RemoteCall\MockPolicies\MockResponseAltPolicy.cs
using Kurisu.RemoteCall.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;

namespace Kurisu.Test.RemoteCall.MockPolicies;

public class MockResponseAltPolicy : IRemoteCallPolicyHandler
{
    public IHttpClientBuilder ConfigureHttpClient(IHttpClientBuilder builder)
    {
        return builder.ConfigurePrimaryHttpMessageHandler(() =>
        {
            var mock = new MockHttpMessageHandler();

            // Respond with plain text that does not contain a "value" field
            mock.When(HttpMethod.Get, "http://localhost:5005/api/resp/alt")
                .Respond("text/plain", "plain-alt-response");

            return mock;
        });
    }
}
