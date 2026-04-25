using System.Net;
using System.Text;
using Kurisu.RemoteCall.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;

namespace Kurisu.Test.RemoteCall.MockPolicies;

public class MockContentPolicy : IRemoteCallPolicyHandler
{
    public IHttpClientBuilder ConfigureHttpClient(IHttpClientBuilder builder)
    {
        return builder.ConfigurePrimaryHttpMessageHandler(() =>
        {
            var mock = new MockHttpMessageHandler();

            mock.When(HttpMethod.Post, "http://localhost:5004/api/content/json")
                .Respond(async req =>
                {
                    var body = await req.Content.ReadAsStringAsync();
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(body, Encoding.UTF8, "application/json")
                    };
                });

            mock.When(HttpMethod.Post, "http://localhost:5004/api/content/form")
                .Respond(async req =>
                {
                    var body = await req.Content.ReadAsStringAsync();
                    // echo back the raw body
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(body, Encoding.UTF8, req.Content.Headers.ContentType?.MediaType ?? "text/plain")
                    };
                });

            mock.When(HttpMethod.Post, "http://localhost:5004/api/content/formreal")
                .Respond(async req =>
                {
                    var body = await req.Content.ReadAsStringAsync();
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(body, Encoding.UTF8, req.Content.Headers.ContentType?.MediaType ?? "text/plain")
                    };
                });

            return mock;
        });
    }
}
