using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Kurisu.Elasticsearch.Abstractions;

namespace Kurisu.Elasticsearch.LowLinq
{
    internal class Selectable<TDocument, TResult> : ISelectable<TDocument, TResult> where TDocument : class, new() where TResult : class
    {
        internal ISearchable<TDocument> Searchable { get; set; }
        private Expression<Func<TDocument, TResult>> _selectExpression;

        internal Selectable()
        {

        }

        internal ISelectable<TDocument, TResult> Select(ISearchable<TDocument> searchable, Expression<Func<TDocument, TResult>> selectExpression)
        {
            _selectExpression = selectExpression;
            Searchable = searchable;
            return this;
        }

        internal ISearchable<TDocument> Where(Expression<Func<TDocument, bool>> filterExpression)
        {
            return Searchable.Where(filterExpression);
        }

        internal async Task<List<TDocument>> ToListAsync()
        {
            return null;
        }
    }
}
