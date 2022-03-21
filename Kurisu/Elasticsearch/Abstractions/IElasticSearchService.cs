using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elasticsearch.Net;
using Kurisu.DependencyInjection.Abstractions;
using Nest;

namespace Kurisu.Elasticsearch.Abstractions
{
    /// <summary>
    /// elastic search service
    /// </summary>
    public interface IElasticSearchService : ITransientDependency
    {
        /// <summary>
        /// linq search client
        /// </summary>
        public IElasticClient Client { get; set; }

        public string Index { get; }
    }
}
