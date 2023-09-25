using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace Kurisu.EFSharding.Sharding.Visitors;

[ExcludeFromCodeCoverage]
internal class RemoveOrderByVisitor : ExpressionVisitor
{
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.Name == nameof(Queryable.OrderBy))
            return base.Visit(node.Arguments[0]);

        return base.VisitMethodCall(node);
    }
}