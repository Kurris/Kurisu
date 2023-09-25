using System.Linq.Expressions;
using Kurisu.EFSharding.Exceptions;
using Kurisu.EFSharding.Sharding.MergeContexts;
using Kurisu.EFSharding.Sharding.ShardingExecutors.Abstractions;
using Kurisu.EFSharding.Sharding.Visitors.Selects;
using Kurisu.EFSharding.Extensions;

namespace Kurisu.EFSharding.Sharding.Visitors;

internal class QueryableExtraDiscoverVisitor : ShardingExpressionVisitor
{
    private readonly IMergeQueryCompilerContext _mergeQueryCompilerContext;
    private GroupByContext _groupByContext = new();
    private SelectContext _selectContext = new();
    private PaginationContext _paginationContext = new();
    private OrderByContext _orderByContext = new();

    public QueryableExtraDiscoverVisitor(IMergeQueryCompilerContext mergeQueryCompilerContext)
    {
        _mergeQueryCompilerContext = mergeQueryCompilerContext;
    }
    public SelectContext GetSelectContext()
    {
        return _selectContext;
    }

    public GroupByContext GetGroupByContext()
    {
        return _groupByContext;
    }

    public PaginationContext GetPaginationContext()
    {
        var fixedTake = _mergeQueryCompilerContext.GetFixedTake();
        if (fixedTake.HasValue)
        {
            _paginationContext.ReplaceToFixedTake(fixedTake.Value);
        }
        return _paginationContext;
    }
    public OrderByContext GetOrderByContext()
    {
        return _orderByContext;
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        var method = node.Method;
        if (node.Method.Name == nameof(Queryable.Skip))
        {
            if (_paginationContext.HasSkip())
                throw new ShardingCoreInvalidOperationException("more than one skip found");
            var skip = (int)GetExpressionValue(node.Arguments[1]);
            _paginationContext.AddSkip(skip);
        }
        else if (node.Method.Name == nameof(Queryable.Take))
        {
            if (_paginationContext.HasTake())
                throw new ShardingCoreInvalidOperationException("more than one take found");
            var take = (int)GetExpressionValue(node.Arguments[1]);
            _paginationContext.AddTake(take);
        }
        else if (method.Name == nameof(Queryable.OrderBy) || method.Name == nameof(Queryable.OrderByDescending) || method.Name == nameof(Queryable.ThenBy) || method.Name == nameof(Queryable.ThenByDescending))
        {
            if (typeof(IOrderedQueryable).IsAssignableFrom(node.Type))
            {
                MemberExpression expression =null;
                var orderbody = ((node.Arguments[1] as UnaryExpression).Operand as LambdaExpression).Body;
                if(orderbody is MemberExpression orderMemberExpression)
                {
                    expression = orderMemberExpression;
                }
                else if (orderbody.NodeType == ExpressionType.Convert&&orderbody is UnaryExpression orderUnaryExpression)
                {
                    if(orderUnaryExpression.Operand is MemberExpression orderMemberConvertExpression)
                    {
                        expression = orderMemberConvertExpression;
                    }
                }
                if (expression == null)
                    throw new NotSupportedException("sharding order not support ");
                List<string> properties = new List<string>();
                GetPropertyInfo(properties, expression);
                if (!properties.Any())
                    throw new NotSupportedException("sharding order only support property expression");
                properties.Reverse();
                var propertyExpression = string.Join(".", properties);
                _orderByContext.PropertyOrders.AddFirst(new PropertyOrder(propertyExpression, method.Name == nameof(Queryable.OrderBy) || method.Name == nameof(Queryable.ThenBy), expression.Member.DeclaringType));

            }
        }
        else if (node.Method.Name == nameof(Queryable.GroupBy))
        {
            if (_groupByContext.GroupExpression == null)
            {
                var expression = (node.Arguments[1] as UnaryExpression).Operand as LambdaExpression;
                if (expression == null)
                    throw new NotSupportedException("sharding group not support ");
                _groupByContext.GroupExpression = expression;
            }
        }
        else if (node.Method.Name == nameof(Queryable.Select))
        {
            if (_selectContext.SelectProperties.IsEmpty())
            {
                var expression = ((node.Arguments[1] as UnaryExpression).Operand as LambdaExpression).Body;
                if (expression is NewExpression newExpression)
                {
                    var aggregateDiscoverVisitor = new QuerySelectDiscoverVisitor(_selectContext);
                    aggregateDiscoverVisitor.Visit(newExpression);
                } else if (expression is MemberExpression memberExpression)
                {

                    var declaringType = memberExpression.Member.DeclaringType;
                    var memberName = memberExpression.Member.Name;
                    var propertyInfo = declaringType.GetUltimateShadowingProperty(memberName);
                    _selectContext.SelectProperties.Add(new SelectOwnerProperty(declaringType, propertyInfo));
                    //memberExpression.Acc
                }else if (expression is MemberInitExpression memberInitExpression)
                {
                    foreach (var memberBinding in memberInitExpression.Bindings)
                    {
                        if (memberBinding is MemberAssignment memberAssignment)
                        {
                            if (memberAssignment.Expression is MemberExpression bindMemberExpression)
                            {
                                var declaringType = memberBinding.Member.DeclaringType;
                                var memberName = memberBinding.Member.Name;
                                var propertyInfo = declaringType.GetUltimateShadowingProperty(memberName);
                                _selectContext.SelectProperties.Add(new SelectOwnerProperty(declaringType, propertyInfo));
                            }
                        }
                    }
                }
                //if (expression != null)
                //{
                //    var aggregateDiscoverVisitor = new QuerySelectDiscoverVisitor(_selectContext);
                //    aggregateDiscoverVisitor.Visit(expression);
                //}
            }
        }

        return base.VisitMethodCall(node);
    }
    private void GetPropertyInfo(List<string> properties, MemberExpression memberExpression)
    {
        properties.Add(memberExpression.Member.Name);
        if (memberExpression.Expression is MemberExpression member)
        {
            GetPropertyInfo(properties, member);
        }
    }



}