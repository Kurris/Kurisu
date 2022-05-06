using System.Collections.Generic;
using Kurisu.Elasticsearch.Abstractions;

namespace Kurisu.Elasticsearch.LowLinq.Syntax
{
    internal class EsItem : BaseElasticSearchOperation
    {
        public EsItem(Dictionary<string,object> dic)
        {
            _properties = dic;
        }
    }
}

