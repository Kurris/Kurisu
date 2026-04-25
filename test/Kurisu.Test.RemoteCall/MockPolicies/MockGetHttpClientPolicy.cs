using System.Net;
using System.Text;
using System.Web;
using Kurisu.RemoteCall.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using RichardSzalay.MockHttp;

namespace Kurisu.Test.RemoteCall.MockPolicies;

public class MockGetHttpClientPolicy : IRemoteCallPolicyHandler
{
    public IHttpClientBuilder ConfigureHttpClient(IHttpClientBuilder builder)
    {
        return builder.ConfigurePrimaryHttpMessageHandler(() =>
        {
            var mock = new MockHttpMessageHandler();
            mock.When(HttpMethod.Get, "http://localhost:5000/api/get1")
                .Respond(httpRequest =>
                {
                    var uri = httpRequest.RequestUri!;

                    var queryParameters = HttpUtility.ParseQueryString(uri.Query);
                    var type = Convert.ToInt32(queryParameters["type"]);
                    var name = queryParameters["name"];

                    HttpStatusCode statusCode = HttpStatusCode.OK;

                    // 4. 创建并返回 HttpResponseMessage
                    return new HttpResponseMessage(statusCode)
                    {
                        Content = new StringContent(new
                        {
                            name,
                            type
                        }.ToJson(), Encoding.UTF8, "application/json")
                    };
                });


            mock.When(HttpMethod.Get, "http://localhost:5000/api/get2")
                .Respond(httpRequest =>
                {
                    var uri = httpRequest.RequestUri!;

                    var queryParameters = HttpUtility.ParseQueryString(uri.Query);
                    var type = Convert.ToInt32(queryParameters["Type"]);
                    var name = queryParameters["Name"];

                    HttpStatusCode statusCode = HttpStatusCode.OK;

                    // 4. 创建并返回 HttpResponseMessage
                    return new HttpResponseMessage(statusCode)
                    {
                        Content = new StringContent(new
                        {
                            Name = name,
                            Type = type
                        }.ToJson(), Encoding.UTF8, "application/json")
                    };
                });

            // mock.When(HttpMethod.Get, "http://localhost:5000/api/get2")
            //     .Respond( httpRequest =>
            //     {
            //         // 1. 读取请求体内容
            //         var requestContent =  httpRequest.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            //
            //         var jObject = JObject.Parse(requestContent);
            //         var name = jObject["Name"].Value<string>();
            //         var type = jObject["Type"].Value<int>();
            //
            //         return new HttpResponseMessage(HttpStatusCode.OK)
            //         {
            //             Content = new StringContent(new
            //             {
            //                 Name = name,
            //                 Type = type
            //             }.ToJson(), Encoding.UTF8, "application/json")
            //         };
            //     });

            return mock;
        });
    }
}