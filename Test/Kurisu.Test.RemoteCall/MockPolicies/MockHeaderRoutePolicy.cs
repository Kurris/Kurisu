using System.Net;
using System.Text;
using Kurisu.RemoteCall.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;
using System.Linq;
using System.Web;

namespace Kurisu.Test.RemoteCall.MockPolicies;

public class MockHeaderRoutePolicy : IRemoteCallPolicyHandler
{
    public IHttpClientBuilder ConfigureHttpClient(IHttpClientBuilder builder)
    {
        return builder.ConfigurePrimaryHttpMessageHandler(() =>
        {
            var mock = new MockHttpMessageHandler();

            mock.When(HttpMethod.Get, "http://localhost:5002/api/items/*")
                .Respond(req =>
                {
                    var uri = req.RequestUri!;
                    // id is part of path
                    var id = HttpUtility.ParseQueryString(uri.Query).Get("id");
                    var header = req.Headers.Contains("X-Custom") ? string.Join(",", req.Headers.GetValues("X-Custom")) : string.Empty;
                    var resp = new { id = id.Trim('/'), header };
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(resp.ToJson(), Encoding.UTF8, "application/json")
                    };
                });

            return mock;
        });
    }
}
