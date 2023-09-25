using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using Kurisu.EFSharding.Core.TrackerManagers;
using Kurisu.EFSharding.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace Kurisu.EFSharding.Sharding.Visitors.Querys;

/// <summary>
/// 获取分表类型
/// </summary>
internal class QueryEntitiesVisitor : ExpressionVisitor
{
    private readonly ITrackerManager _trackerManager;
    private readonly ISet<Type> _shardingEntities = new HashSet<Type>();

    public QueryEntitiesVisitor(ITrackerManager trackerManager)
    {
        _trackerManager = trackerManager;
    }

    public ISet<Type> GetQueryEntities()
    {
        return _shardingEntities;
    }

    protected override Expression VisitExtension(Expression node)
    {
        if (node is QueryRootExpression queryRootExpression)
        {
#if EFCORE7
                _shardingEntities.Add(queryRootExpression.ElementType);
#else
            _shardingEntities.Add(queryRootExpression.EntityType.ClrType);
#endif
        }

        return base.VisitExtension(node);
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        var methodName = node.Method.Name;
        if (methodName == nameof(EntityFrameworkQueryableExtensions.Include) || methodName == nameof(EntityFrameworkQueryableExtensions.ThenInclude))
        {
            var genericArguments = node.Type.GetGenericArguments();
            for (var i = 0; i < genericArguments.Length; i++)
            {
                var genericArgument = genericArguments[i];
                if (typeof(IEnumerable).IsAssignableFrom(genericArgument))
                {
                    var arguments = genericArgument.GetGenericArguments();
                    foreach (var argument in arguments)
                    {
                        //if is db context model
                        if (_trackerManager.IsDbContextModel(argument))
                        {
                            _shardingEntities.Add(argument);
                        }
                    }
                }

                if (!genericArgument.IsSimpleType())
                {
                    //if is db context model
                    if (_trackerManager.IsDbContextModel(genericArgument))
                    {
                        _shardingEntities.Add(genericArgument);
                    }
                }
            }
        }

        return base.VisitMethodCall(node);
    }

    protected override Expression VisitMember
        (MemberExpression memberExpression)
    {
        // Recurse down to see if we can simplify...
        var expression = Visit(memberExpression.Expression);

        // If we've ended up with a constant, and it's a property or a field,
        // we can simplify ourselves to a constant
        if (expression is ConstantExpression)
        {
            object container = ((ConstantExpression) expression).Value;
            var member = memberExpression.Member;
            if (member is FieldInfo fieldInfo)
            {
                object value = fieldInfo.GetValue(container);
                if (value is IQueryable queryable)
                {
                    _shardingEntities.Add(queryable.ElementType);
                }
                //return Expression.Constant(value);
            }

            if (member is PropertyInfo propertyInfo)
            {
                object value = propertyInfo.GetValue(container, null);
                if (value is IQueryable queryable)
                {
                    _shardingEntities.Add(queryable.ElementType);
                }
            }
        }

        return base.VisitMember(memberExpression);
    }
}