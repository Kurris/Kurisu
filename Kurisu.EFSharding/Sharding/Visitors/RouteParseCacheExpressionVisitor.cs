using System.Linq.Expressions;

namespace Kurisu.EFSharding.Sharding.Visitors;

internal class RouteParseCacheExpressionVisitor : ExpressionVisitor
{
    private bool _hasOrElse;
    private int _andAlsoCount;
    private int _equalCount;
    protected override Expression VisitBinary(BinaryExpression node)
    {
        if (node.NodeType == ExpressionType.OrElse)
        {
            if (!_hasOrElse)
            {
                _hasOrElse = true;
            }
        }else if (node.NodeType == ExpressionType.AndAlso)
        {
            _andAlsoCount++;
        }else if (node.NodeType == ExpressionType.Equal)
        {
            _equalCount++;
        }
        return base.VisitBinary(node);
    }

    public bool HasOrElse()
    {
        return _hasOrElse;
    }

    public int AndAlsoCount()
    {
        return _andAlsoCount;
    }
    public int EqualCount()
    {
        return _equalCount;
    }
}