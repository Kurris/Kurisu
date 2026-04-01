//using System.Linq.Expressions;

//namespace Kurisu.Extensions.SqlSugar.Sharding;

//public class ExpressionHelper<T>
//{
//    private readonly Expression<Func<T, object>> _orderBy;

//    public ExpressionHelper(Expression<Func<T, object>> orderBy)
//    {
//        _orderBy = orderBy;
//    }

//    public Expression<Func<T, bool>> GetEqual<TValue>(string property, TValue value)
//    {
//        // build dynamic expression based on route key (comparison depends on order)
//        var param = Expression.Parameter(typeof(T), "x");
//        var member = Expression.Property(param, property);
//        // ensure constant has compatible type (handle Nullable<DateTime>)
//        var valueExp = Expression.Constant(value, value?.GetType() ?? typeof(TValue));
//        Expression constConverted = valueExp;
//        if (valueExp.Type != member.Type)
//        {
//            constConverted = Expression.Convert(valueExp, member.Type);
//        }

//        Expression compare;
//        // if order is descending, we want member <= value (e.g., Desc order earlier), else member >= value
//        if (IsFieldDescending(property))
//            compare = Expression.LessThanOrEqual(member, constConverted);
//        else
//            compare = Expression.GreaterThanOrEqual(member, constConverted);

//        var dynamicExpression = Expression.Lambda<Func<T, bool>>(compare, param);

//        return dynamicExpression;
//    }

//    public bool IsFieldDescending(string property)
//    {
//        if (_orderBy == null) return false; // default ascending

//        Expression body = _orderBy.Body;
//        // unwrap Convert
//        if (body is UnaryExpression ue && ue.NodeType == ExpressionType.Convert)
//            body = ue.Operand;

//        // anonymous object: NewExpression
//        if (body is NewExpression ne)
//        {
//            for (int i = 0; i < ne.Arguments.Count; i++)
//            {
//                var arg = ne.Arguments[i];
//                var memberName = ne.Members?[i]?.Name;

//                // match by anonymous property name
//                if (string.Equals(memberName, property, StringComparison.OrdinalIgnoreCase))
//                {
//                    // if arg is method call like SqlFunc.Desc(x.CreateTime)
//                    if (arg is MethodCallExpression mce)
//                    {
//                        var methodName = mce.Method.Name.ToLowerInvariant();
//                        if (methodName.Contains("desc")) return true;
//                        if (methodName.Contains("asc")) return false;
//                    }

//                    // if arg is member access x.CreateTime
//                    if (arg is MemberExpression me)
//                    {
//                        if (string.Equals(me.Member.Name, property, StringComparison.OrdinalIgnoreCase))
//                        {
//                            return false; // default ascending
//                        }
//                    }

//                    // other cases default
//                    return false;
//                }

//                // also try to match by inner member name if arg is MemberExpression
//                if (arg is MemberExpression ame)
//                {
//                    if (string.Equals(ame.Member.Name, property, StringComparison.OrdinalIgnoreCase))
//                    {
//                        // check if corresponding anonymous member was given a SqlFunc wrapper
//                        if (ne.Arguments[i] is MethodCallExpression mm)
//                        {
//                            var methodName = mm.Method.Name.ToLowerInvariant();
//                            if (methodName.Contains("desc")) return true;
//                            if (methodName.Contains("asc")) return false;
//                        }

//                        return false;
//                    }
//                }
//            }
//        }

//        // direct member: x => x.CreateTime or Convert(x.CreateTime)
//        if (body is MemberExpression mem)
//        {
//            if (string.Equals(mem.Member.Name, property, StringComparison.OrdinalIgnoreCase))
//                return false;
//        }

//        return false;
//    }

//    public void ProcessExpression(Expression expression, string routeKey, ref DateTime? start, ref DateTime? end)
//    {
//        if (expression is BinaryExpression be)
//        {
//            // handle logical AND
//            if (be.NodeType == ExpressionType.AndAlso || be.NodeType == ExpressionType.And)
//            {
//                ProcessExpression(be.Left, routeKey, ref start, ref end);
//                ProcessExpression(be.Right, routeKey, ref start, ref end);
//                return;
//            }

//            // normalize member on left and constant/value on right
//            MemberExpression member = null;
//            Expression valueExp = null;

//            if (be.Left is MemberExpression leftMember)
//            {
//                member = leftMember;
//                valueExp = be.Right;
//            }
//            else if (be.Right is MemberExpression rightMember)
//            {
//                member = rightMember;
//                valueExp = be.Left;
//            }

//            if (member != null && member.Member.Name == routeKey)
//            {
//                // evaluate valueExp to actual value
//                object value = null;
//                try
//                {
//                    if (valueExp != null)
//                    {
//                        var lambda = Expression.Lambda(valueExp);
//                        value = lambda.Compile().DynamicInvoke();
//                    }
//                }
//                catch
//                {
//                    value = null;
//                }

//                if (value is DateTime dt)
//                {
//                    switch (be.NodeType)
//                    {
//                        case ExpressionType.GreaterThanOrEqual:
//                            if (start == null || dt > start) start = dt;
//                            break;
//                        case ExpressionType.GreaterThan:
//                            // exclusive, move one tick forward
//                            var gt = dt.AddTicks(1);
//                            if (start == null || gt > start) start = gt;
//                            break;
//                        case ExpressionType.LessThanOrEqual:
//                            if (end == null || dt < end) end = dt;
//                            break;
//                        case ExpressionType.LessThan:
//                            var lt = dt.AddTicks(-1);
//                            if (end == null || lt < end) end = lt;
//                            break;
//                        case ExpressionType.Equal:
//                            if (start == null || dt > start) start = dt;
//                            if (end == null || dt < end) end = dt;
//                            break;
//                    }
//                }
//            }
//        }
//        else if (expression is UnaryExpression ue)
//        {
//            ProcessExpression(ue.Operand, routeKey, ref start, ref end);
//        }
//        else if (expression is MethodCallExpression mce)
//        {
//            // could be calls like .Between or other methods; ignore for now
//        }
//    }

//    public Expression<Func<T, bool>> AndAlso(Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
//    {
//        var param = Expression.Parameter(typeof(T), "x");
//        var leftBody = ReplaceParameter(left.Body, left.Parameters[0], param);
//        var rightBody = ReplaceParameter(right.Body, right.Parameters[0], param);
//        var body = Expression.AndAlso(leftBody, rightBody);
//        return Expression.Lambda<Func<T, bool>>(body, param);
//    }

//    private static Expression ReplaceParameter(Expression expression, ParameterExpression toReplace, ParameterExpression replaceWith)
//    {
//        return new ParameterReplacer(toReplace, replaceWith).Visit(expression);
//    }

//    private class ParameterReplacer : ExpressionVisitor
//    {
//        private readonly ParameterExpression _from;
//        private readonly ParameterExpression _to;

//        public ParameterReplacer(ParameterExpression from, ParameterExpression to)
//        {
//            _from = from;
//            _to = to;
//        }

//        protected override Expression VisitParameter(ParameterExpression node)
//        {
//            if (node == _from) return _to;
//            return base.VisitParameter(node);
//        }
//    }

//    //public Expression<Func<T, TempResult<TSelect>>> GetAggregateSelector<TSelect>(string property)
//    //{
//    //    var param = Expression.Parameter(typeof(T), "x");
//    //    var member = Expression.PropertyOrField(param, property);

//    //    // find generic AggregateMax/Min methods
//    //    var aggMaxMethod = typeof(SqlFunc).GetMethods(BindingFlags.Public | BindingFlags.Static)
//    //        .FirstOrDefault(m => m.Name == "AggregateMax" && m.IsGenericMethod && m.GetParameters().Length == 1);
//    //    var aggMinMethod = typeof(SqlFunc).GetMethods(BindingFlags.Public | BindingFlags.Static)
//    //        .FirstOrDefault(m => m.Name == "AggregateMin" && m.IsGenericMethod && m.GetParameters().Length == 1);

//    //    if (aggMaxMethod == null || aggMinMethod == null)
//    //        throw new InvalidOperationException("Cannot find SqlFunc.AggregateMax/AggregateMin methods via reflection.");

//    //    var genericMax = aggMaxMethod.MakeGenericMethod(member.Type);
//    //    var genericMin = aggMinMethod.MakeGenericMethod(member.Type);

//    //    var callMax = Expression.Call(genericMax, member);
//    //    var callMin = Expression.Call(genericMin, member);

//    //    var resultType = typeof(TempResult<>).MakeGenericType(member.Type);
//    //    var newExpr = Expression.MemberInit(Expression.New(resultType),
//    //        Expression.Bind(resultType.GetProperty("Max"), callMax),
//    //        Expression.Bind(resultType.GetProperty("Min"), callMin));

//    //    var lambdaType = typeof(Func<,>).MakeGenericType(typeof(T), resultType);
//    //    var lambda = Expression.Lambda(lambdaType, newExpr, param);

//    //    return (Expression<Func<T, TempResult<TSelect>>>)lambda;
//    //}

//    public Expression<Func<T, TSelectType>> GetSelect<TSelectType>(string property)
//    {
//        var paramForSelect = Expression.Parameter(typeof(T), "x");
//        var memberForSelect = Expression.PropertyOrField(paramForSelect, property);
//        Expression convertedForSelect = memberForSelect;

//        var underlying = Nullable.GetUnderlyingType(memberForSelect.Type);
//        if (underlying == typeof(TSelectType))
//        {
//            convertedForSelect = Expression.Convert(memberForSelect, typeof(TSelectType));
//        }
//        var selectExpr = Expression.Lambda<Func<T, TSelectType>>(convertedForSelect, paramForSelect);

//        return selectExpr;
//    }


//    private (DateTime? start, DateTime? end) ExtractDateRangeFromExpressions(List<Expression<Func<T, bool>>> wheres, string routeKey)
//    {
//        DateTime? start = null;
//        DateTime? end = null;

//        foreach (var expr in wheres)
//        {
//            ProcessExpression(expr.Body, routeKey, ref start, ref end);
//        }

//        return (start, end);
//    }
//}



