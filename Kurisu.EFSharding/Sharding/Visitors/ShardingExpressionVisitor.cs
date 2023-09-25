using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using Kurisu.EFSharding.Exceptions;
using Kurisu.EFSharding.Extensions;

namespace Kurisu.EFSharding.Sharding.Visitors;

public abstract class ShardingExpressionVisitor : ExpressionVisitor
{
    protected object GetExpressionValue(Expression expression)
    {
        switch (expression)
        {
            case ConstantExpression e:
                return e.Value;
            case NewExpression e:
                return e.Constructor?.Invoke(e.Arguments.Select(a => ((ConstantExpression) a).Value).ToArray());

            case MemberExpression e when e.Member is FieldInfo field:
                return field.GetValue(
                    GetExpressionValue(
                        e.Expression
                    ) ?? throw new InvalidOperationException(
                        $"cant get expression value,{e.Expression.ShardingPrint()} may be null reference")
                );

            case MemberExpression e when e.Member is PropertyInfo property:
            {
                if (e.Expression == null)
                {
                    if (property.DeclaringType == typeof(DateTime) && property.Name == nameof(DateTime.Now))
                    {
                        return DateTime.Now;
                    }

                    if (property.DeclaringType == typeof(DateTimeOffset) &&
                        property.Name == nameof(DateTimeOffset.Now))
                    {
                        return DateTimeOffset.Now;
                    }
                }

                return property.GetValue(
                    GetExpressionValue(
                        e.Expression
                    ) ?? throw new InvalidOperationException(
                        $"cant get expression value,{e.Expression.ShardingPrint()} may be null reference")
                );
            }

            case ListInitExpression e when e.NewExpression.Arguments.Count() == 0:
            {
                var collection = e.NewExpression.Constructor.Invoke(new object[0]);
                foreach (var i in e.Initializers)
                {
                    i.AddMethod.Invoke(
                        collection,
                        i.Arguments
                            .Select(
                                a => GetExpressionValue(a)
                            )
                            .ToArray()
                    );
                }

                return collection;
            }
            case NewArrayExpression e when e.NodeType == ExpressionType.NewArrayInit && e.Expressions.Count > 0:
            {
                var collection = new List<object>(e.Expressions.Count);
                foreach (var arrayItemExpression in e.Expressions)
                {
                    collection.Add(GetExpressionValue(arrayItemExpression));
                }

                return collection;
            }


            case MethodCallExpression e:
            {
                var expressionValue = GetExpressionValue(e.Object);

                return e.Method.Invoke(
                    expressionValue,
                    e.Arguments
                        .Select(
                            a => GetExpressionValue(a)
                        )
                        .ToArray()
                );
            }
            case UnaryExpression e when e.NodeType == ExpressionType.Convert:
            {
                return GetExpressionValue(e.Operand);
            }

            default:
            {
                if (expression is BinaryExpression binaryExpression &&
                    expression.NodeType == ExpressionType.ArrayIndex)
                {
                    var index = GetExpressionValue(binaryExpression.Right);
                    if (index is int i)
                    {
                        var arrayObject = GetExpressionValue(binaryExpression.Left);
                        if (arrayObject is IList list)
                        {
                            return list[i];
                        }
                    }
                }

                //TODO: better messaging
                throw new ShardingCoreException("cant get value " + expression);
            }
        }
    }
}