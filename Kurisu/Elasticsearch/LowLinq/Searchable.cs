using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Elasticsearch.Net;
using Kurisu.Common.Dto;
using Kurisu.Elasticsearch.Abstractions;
using Kurisu.Elasticsearch.LowLinq.Syntax;
using Kurisu.Utils.Extensions;
using Nest;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Kurisu.Elasticsearch.LowLinq
{
    internal class Searchable<TDocument> : BaseElasticSearchOperation, ISearchable<TDocument>, IEsSortable where TDocument : class, new()
    {
        private readonly IElasticLowLevelClient _client;
        private string _index;
        private readonly PostData _postData;
        private Expression<Func<TDocument, bool>> _searchExpression;

        private Dictionary<string, int> _page;

        public IDictionary<string, object> Orders { get; set; }

        internal Searchable(IElasticSearchService elasticSearchService, string index = null, PostData postData = null)
        {
            _client = elasticSearchService.Client.LowLevel;
            _index = index.ToLower();
            _postData = postData;
        }

        public async Task<List<TDocument>> ToListAsync()
        {
            var res = await BuildAsync();
            return res.ToList();
        }

        public ISearchable<TDocument> Where(Expression<Func<TDocument, bool>> filterExpression)
        {
            if (_searchExpression == null)
            {
                _searchExpression = filterExpression;
            }
            else
            {
                _searchExpression = _searchExpression.And(filterExpression);
            }


            return this;
        }


        //public Task<Pagination<TDocument>> ToPagination(PageIn pageIn)
        //{
        //    _page = new EsPage().Build(pageIn);
        //}

 

        private async Task<IReadOnlyCollection<TDocument>> BuildAsync()
        {
            //条件过滤
            if (_searchExpression != null)
            {
                var translate = new EsTranslate();
                var query= translate.GetQuery(_searchExpression);
                _properties.Add("query", query);
            }

            //排序
            if (Orders?.Count > 0)
            {
                var order = Orders.First();
                _properties.Add(order.Key, new List<object> {order.Value});
            }


            //分页
            if (_page?.Count > 0)
            {
                foreach (var (key, value) in _page)
                {
                    _properties.Add(key, value);
                }
            }

            dynamic dy = this;

            var searchBody = JsonConvert.SerializeObject(dy);

#if DEBUG
            Console.WriteLine($@" 
GET {_index}/_search
{ConvertJsonString(searchBody)}");
#endif


            var response = await _client.SearchAsync<SearchResponse<TDocument>>(_index, PostData.String(searchBody));
            return response.Documents;
        }

        private string ConvertJsonString(string str)
        {
            //格式化json字符串
            JsonSerializer serializer = new JsonSerializer();
            TextReader tr = new StringReader(str);
            JsonTextReader jtr = new JsonTextReader(tr);
            object obj = serializer.Deserialize(jtr);
            if (obj != null)
            {
                StringWriter textWriter = new StringWriter();
                JsonTextWriter jsonWriter = new JsonTextWriter(textWriter)
                {
                    Formatting = Formatting.Indented,
                    Indentation = 4,
                    IndentChar = ' '
                };
                serializer.Serialize(jsonWriter, obj);
                return textWriter.ToString();
            }
            else
            {
                return str;
            }
        }
    }
}
