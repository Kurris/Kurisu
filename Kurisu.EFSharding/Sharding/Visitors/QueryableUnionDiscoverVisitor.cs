using System.Linq.Expressions;

namespace Kurisu.EFSharding.Sharding.Visitors;

internal class QueryableUnionDiscoverVisitor:ExpressionVisitor
{
    public bool IsUnion { get; private set; }
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.Name == nameof(Queryable.Union))
        {
            IsUnion = true;
        }

        return base.VisitMethodCall(node);
    }
}