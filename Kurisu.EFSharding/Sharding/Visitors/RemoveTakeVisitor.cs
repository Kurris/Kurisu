using System.Linq.Expressions;

namespace Kurisu.EFSharding.Sharding.Visitors;

/// <summary>
/// 删除Take表达式
/// </summary>
internal class RemoveTakeVisitor : ExpressionVisitor
{
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.Name == nameof(Queryable.Take))
            return base.Visit(node.Arguments[0]);

        return base.VisitMethodCall(node);
    }
}