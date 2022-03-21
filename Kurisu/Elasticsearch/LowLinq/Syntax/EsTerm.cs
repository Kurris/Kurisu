using System.Collections.Generic;
using Kurisu.Elasticsearch.Abstractions;
using Newtonsoft.Json;

namespace Kurisu.Elasticsearch.LowLinq.Syntax
{
    internal class EsTerm : BaseElasticSearchOperation
    {
        public EsTerm(Dictionary<string, object> dic)
        {
            _properties = dic;
        }
    }

    internal class EsValue
    {
        [JsonProperty("value")]
        public object Value { get; set; }
    }
}
