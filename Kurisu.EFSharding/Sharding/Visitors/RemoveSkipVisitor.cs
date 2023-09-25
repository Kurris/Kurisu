using System.Linq.Expressions;

namespace Kurisu.EFSharding.Sharding.Visitors;

/// <summary>
/// 删除Skip表达式
/// </summary>
internal class RemoveSkipVisitor : ExpressionVisitor
{
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.Name == nameof(Queryable.Skip))
            return base.Visit(node.Arguments[0]);

        return base.VisitMethodCall(node);
    }
}