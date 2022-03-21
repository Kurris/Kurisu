using System;
using Elasticsearch.Net;
using Kurisu.Elasticsearch.Abstractions;
using Nest;

namespace Kurisu.Elasticsearch.Implements;

public class ElasticSearchService : IElasticSearchService
{

    public ElasticSearchService()
    {
        var uris = new[]
        {
            new Uri("http://192.168.1.4:31540/"),
        };

        var nodes = new StaticConnectionPool(uris);
        var setting = new ConnectionSettings(nodes).RequestTimeout(TimeSpan.FromSeconds(30));

        //如果需要密码
        //BasicAuthentication()


        this.Client = new ElasticClient(setting);
    }



    public IElasticClient Client { get; set; }
    public string Index { get; }
}