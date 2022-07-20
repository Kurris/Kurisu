using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Kurisu.Elasticsearch.LowLinq.Syntax;

namespace Kurisu.Elasticsearch.Abstractions
{
    public interface ISearchable<TDocument> where TDocument : class, new()
    {
        Task<List<TDocument>> ToListAsync();

        ISearchable<TDocument> Where(Expression<Func<TDocument, bool>> filterExpression);
        //Task<Pagination<TDocument>> ToPagination(PageIn pageIn);
    }
}
