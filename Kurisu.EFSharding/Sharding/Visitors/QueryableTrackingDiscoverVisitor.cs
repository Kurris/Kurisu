using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Kurisu.EFSharding.Sharding.Visitors;

internal class QueryableTrackingDiscoverVisitor : ExpressionVisitor
{
    public bool? IsNoTracking { get; private set; }
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.Name == nameof(EntityFrameworkQueryableExtensions.AsNoTracking))
        {
            IsNoTracking = true;
        }
        else if (node.Method.Name == nameof(EntityFrameworkQueryableExtensions.AsTracking))
        {
            IsNoTracking = false;
        }

        return base.VisitMethodCall(node);
    }
}