using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Kurisu.Elasticsearch.Abstractions
{
    public interface IOrderedList<TDocument> where TDocument : class, new()
    {
        ISearchable<TDocument> Searchable { get; }

        IOrderedList<TDocument> OrderBy<TKey>(Expression<Func<TDocument, TKey>> orderExpression);

        IOrderedList<TDocument> OrderByDescending<TKey>(Expression<Func<TDocument, TKey>> orderExpression);

        IOrderedList<TDocument> ThenBy<TKey>(Expression<Func<TDocument, TKey>> orderExpression);

        IOrderedList<TDocument> ThenByDescending<TKey>(Expression<Func<TDocument, TKey>> orderExpression);
    }
}
