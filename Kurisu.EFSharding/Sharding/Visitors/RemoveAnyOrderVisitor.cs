using System.Linq.Expressions;

namespace Kurisu.EFSharding.Sharding.Visitors;

public class RemoveAnyOrderVisitor: ExpressionVisitor
{
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        return node.Method.Name switch
        {
            nameof(Queryable.OrderBy) => base.Visit(node.Arguments[0]),
            nameof(Queryable.OrderByDescending) => base.Visit(node.Arguments[0]),
            nameof(Queryable.ThenBy) => base.Visit(node.Arguments[0]),
            nameof(Queryable.ThenByDescending) => base.Visit(node.Arguments[0]),
            _ => base.VisitMethodCall(node)
        };
    }
}