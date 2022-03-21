using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Kurisu.Elasticsearch.Abstractions;
using Kurisu.Elasticsearch.LowLinq.Syntax;

namespace Kurisu.Elasticsearch.LowLinq
{
    internal class OrderedList<TDocument> : BaseElasticSearchOperation, IOrderedList<TDocument> where TDocument : class, new()
    {
        private readonly EsTranslate _translate = new();
        private ISearchable<TDocument> _searchable;

        public OrderedList()
        {

        }
        public ISearchable<TDocument> Searchable => _searchable;


        internal IOrderedList<TDocument> OrderBy<TKey>(ISearchable<TDocument> searchable, Expression<Func<TDocument, TKey>> orderExpression)
        {
            _searchable = searchable;

            var orderFields = _translate.GetOrders(orderExpression);
            orderFields.ForEach(x =>
            {
                _properties.TryAdd(x, new EsSort(EsSortType.ASC));
            });

            return this;
        }


        internal IOrderedList<TDocument> OrderByDescending<TKey>(ISearchable<TDocument> searchable, Expression<Func<TDocument, TKey>> orderExpression)
        {
            _searchable = searchable;

            var orderFields = _translate.GetOrders(orderExpression);
            orderFields.ForEach(x =>
            {
                _properties.TryAdd(x, new EsSort(EsSortType.DESC));
            });

            return this;
        }


        public IOrderedList<TDocument> OrderBy<TKey>(Expression<Func<TDocument, TKey>> orderExpression)
        {
            var orderFields = _translate.GetOrders(orderExpression);
            orderFields.ForEach(x =>
            {
                _properties.TryAdd(x, new EsSort(EsSortType.ASC));
            });

            return this;
        }

        public IOrderedList<TDocument> OrderByDescending<TKey>(Expression<Func<TDocument, TKey>> orderExpression)
        {
            var orderFields = _translate.GetOrders(orderExpression);
            orderFields.ForEach(x =>
            {
                _properties.TryAdd(x, new EsSort(EsSortType.DESC));
            });

            return this;
        }


        public IOrderedList<TDocument> ThenBy<TKey>(Expression<Func<TDocument, TKey>> orderExpression)
        {
            var orderFields = _translate.GetOrders(orderExpression);
            orderFields.ForEach(x =>
            {
                _properties.TryAdd(x, new EsSort(EsSortType.ASC));
            });

            return this;
        }

        public IOrderedList<TDocument> ThenByDescending<TKey>(Expression<Func<TDocument, TKey>> orderExpression)
        {
            var orderFields = _translate.GetOrders(orderExpression);
            orderFields.ForEach(x =>
            {
                _properties.TryAdd(x, new EsSort(EsSortType.DESC));
            });

            return this;
        }
    }
}
