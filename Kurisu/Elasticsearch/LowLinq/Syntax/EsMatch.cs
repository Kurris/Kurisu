using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kurisu.Elasticsearch.Abstractions;

namespace Kurisu.Elasticsearch.LowLinq.Syntax
{
    internal class EsMatch : BaseElasticSearchOperation
    {
        /// <summary>
        /// "match": {
        /// "FIELD": VALUE
        /// }
        /// </summary>
        /// <param name="dic"></param>
        public EsMatch(Dictionary<string, object> dic)
        {
            _properties = dic;
        }
    }
}
