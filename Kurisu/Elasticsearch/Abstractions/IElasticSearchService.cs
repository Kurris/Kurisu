using Microsoft.Extensions.DependencyInjection;
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
