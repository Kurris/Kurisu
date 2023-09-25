using System.Linq.Expressions;
using Kurisu.EFSharding.Exceptions;

namespace Kurisu.EFSharding.Extensions;

public static class ShardingComplierExtension
{
    public static Type GetQueryEntityType(this MethodCallExpression expression)
    {
        var rootQuery = expression.Arguments.FirstOrDefault(o => typeof(IQueryable).IsAssignableFrom(o.Type));
        if (rootQuery == null)
            throw new ShardingCoreException("expression error");
        return rootQuery.Type.GetSequenceType();
    }

    public static Type GetResultType(this MethodCallExpression expression)
    {
        if (expression.Arguments.Count == 1)
            return expression.GetQueryEntityType();

        var otherExpression = expression.Arguments.FirstOrDefault(o => !typeof(IQueryable).IsAssignableFrom(o.Type));
        if (otherExpression is UnaryExpression unaryExpression && unaryExpression.Operand is LambdaExpression lambdaExpression)
        {
            return lambdaExpression.ReturnType;
        }

        throw new ShardingCoreException("expression error");
    }
}