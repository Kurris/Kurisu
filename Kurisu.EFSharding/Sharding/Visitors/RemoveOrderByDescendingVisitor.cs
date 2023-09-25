using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace Kurisu.EFSharding.Sharding.Visitors;

[ExcludeFromCodeCoverage]
internal class RemoveOrderByDescendingVisitor : ExpressionVisitor
{
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        return node.Method.Name == nameof(Queryable.OrderByDescending)
            ? base.Visit(node.Arguments[0])
            : base.VisitMethodCall(node);
    }
}