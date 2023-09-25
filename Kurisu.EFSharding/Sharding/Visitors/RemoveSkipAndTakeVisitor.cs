using System.Linq.Expressions;

namespace Kurisu.EFSharding.Sharding.Visitors;

internal class RemoveSkipAndTakeVisitor: ExpressionVisitor
{
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.Name == nameof(Queryable.Skip))
            return base.Visit(node.Arguments[0]);
        if (node.Method.Name == nameof(Queryable.Take))
            return base.Visit(node.Arguments[0]);

        return base.VisitMethodCall(node);
    }
}