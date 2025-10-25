using System.Net;
using System.Text;
using Kurisu.RemoteCall.Abstractions;
using RichardSzalay.MockHttp;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Test.RemoteCall.MockPolicies;

public class MockWeatherHttpClientPolicy : IRemoteCallPolicyHandler
{
    public IHttpClientBuilder ConfigureHttpClient(IHttpClientBuilder builder)
    {
        return builder.ConfigurePrimaryHttpMessageHandler(() =>
        {
            var mock = new MockHttpMessageHandler();

            mock.When(HttpMethod.Get, "http://localhost:5000/api/weather/ping")
                .Respond("application/json", "\"pong\"");

            mock.When(HttpMethod.Get, "http://localhost:5000/api/weather/echo*")
                .Respond(req =>
                {
                    var uri = req.RequestUri!;
                    var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
                    var value = query["value"];
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(value ?? "", Encoding.UTF8, "application/json")
                    };
                });

            mock.When(HttpMethod.Get, "http://localhost:5000/api/weather/list")
                .Respond("application/json", "[{\"Url\":\"/api/weather/1\",\"UserName\":\"user1\"},{\"Url\":\"/api/weather/2\",\"UserName\":\"user2\"}]");

            mock.When(HttpMethod.Post, "http://localhost:5000/api/weather/create")
                .Respond(async req =>
                {
                    var body = await req.Content.ReadAsStringAsync();
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(body, Encoding.UTF8, "application/json")
                    };
                });

            mock.When(HttpMethod.Get, "http://localhost:5000/api/weather/complex*")
                .Respond(req =>
                {
                    var uri = req.RequestUri!;
                    var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
                    var name = query["Name"] ?? query["name"];
                    var idStr = query["Id"] ?? query["id"];
                    var items = new System.Collections.Generic.List<int>();

                    // 支持 Items=1&Items=2 或 Items=1,2
                    var itemValues = query.GetValues("Items") ?? query.GetValues("items");
                    if (itemValues != null)
                    {
                        foreach (var v in itemValues)
                        {
                            if (v.Contains(","))
                            {
                                foreach (var s in v.Split(','))
                                {
                                    if (int.TryParse(s, out var n)) items.Add(n);
                                }
                            }
                            else
                            {
                                if (int.TryParse(v, out var n)) items.Add(n);
                            }
                        }
                    }

                    int.TryParse(idStr, out var id);

                    var resp = new { Name = name, Id = id, Items = items };
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(resp.ToJson(), Encoding.UTF8, "application/json")
                    };
                });

            return mock;
        });
    }
}
