using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Kurisu.AspNetCore.Utils.Extensions;

/// <summary>
/// linq表达式扩展
/// </summary>
public static class LinqExpressionExtensions
{
    /// <summary>
    /// 返回一个默认表达式
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    public static Expression<Func<T, bool>> True<T>()
    {
        return x => true;
    }

    /// <summary>
    /// 返回一个默认表达式
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    public static Expression<Func<T, bool>> False<T>()
    {
        return x => false;
    }

    /// <summary>
    /// 并且
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="predicateCurrent">当前表达式</param>
    /// <param name="predicate">并且的表达式</param>
    public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> predicateCurrent, Expression<Func<T, bool>> predicate) where T : class
    {
        predicateCurrent ??= False<T>();
        return Combine(predicateCurrent, predicate, Expression.AndAlso);
    }

    /// <summary>
    /// 或者
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="predicateCurrent">当前表达式</param>
    /// <param name="predicateAddition">并且的表达式</param>
    public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> predicateCurrent, Expression<Func<T, bool>> predicateAddition) where T : class
    {
        predicateCurrent ??= False<T>();
        return Combine(predicateCurrent, predicateAddition, Expression.OrElse);
    }

    /// <summary>
    /// 否
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="predicateCurrent">当前表达式</param>
    public static Expression<Func<T, bool>> Not<T>(this Expression<Func<T, bool>> predicateCurrent) where T : class
    {
        ExpressionReplace ex = GenExpressionReplace<T>(out var expressionParameter);
        var current = ex.Replace(predicateCurrent);
        var body = Expression.Not(current);

        return Expression.Lambda<Func<T, bool>>(body, expressionParameter);
    }

    /// <summary>
    /// 合并俩个表达式
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="expressionFirst">第一个表达式</param>
    /// <param name="expressionSecond">第二个表达式</param>
    /// <param name="expressionMethod">合并方法</param>
    private static Expression<Func<T, bool>> Combine<T>(Expression<Func<T, bool>> expressionFirst, Expression<Func<T, bool>> expressionSecond, Func<Expression, Expression, Expression> expressionMethod)
    {
        ExpressionReplace ex = GenExpressionReplace<T>(out var expressionParameter);

        var left = ex.Replace(expressionFirst.Body);
        var right = ex.Replace(expressionSecond.Body);

        return Expression.Lambda<Func<T, bool>>(expressionMethod(left, right), expressionParameter);
    }

    /// <summary>
    /// 表达式替换帮助类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="expressionParameter">引用相等的表达式参数</param>
    /// <returns><see cref="ExpressionReplace"/></returns>
    private static ExpressionReplace GenExpressionReplace<T>(out ParameterExpression expressionParameter)
    {
        expressionParameter = Expression.Parameter(typeof(T));
        return new ExpressionReplace(expressionParameter);
    }
}



/// <summary>
/// 表达式构建sql帮助类
/// </summary>
public static class ExpressionHelper
{
    /// <summary>
    /// Ts 获取Or表达式
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="ts">实体集合</param>
    /// <param name="func">表达式方法</param>
    /// <returns></returns>
    public static Expression<Func<T, bool>> GetOrExpression<T>(this IEnumerable<T> ts, Func<T, Expression<Func<T, bool>>> func) where T : class
    {
        var expressions = LinqExpressionExtensions.False<T>();

        return ts.Select(func.Invoke).Aggregate(expressions, (current, exp) => current.Or(exp));
    }

    /// <summary>
    /// 获取实例的表达式
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="name"></param>
    /// <returns></returns>
    public static ParameterExpression GetParameter<T>(string name = null) where T : class
    {
        return Expression.Parameter(typeof(T), name);
    }

    /// <summary>
    /// 枚举比较表达式
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="parameterExpression">实例名</param>
    /// <param name="propertyName">属性名</param>
    /// <param name="value">属性值</param>
    /// <returns></returns>
    public static Expression<Func<T, bool>> GetEnumEquals<T>(this ParameterExpression parameterExpression, string propertyName, object value) where T : class
    {
        var prop = Expression.Property(parameterExpression, propertyName);
        var eq = value.GetType().GetMethods().First(x => x.Name == "Equals");
        var constant = Expression.Constant(value, typeof(object));
        var eqExp = Expression.Call(prop, eq, constant);

        return Expression.Lambda<Func<T, bool>>(eqExp, parameterExpression);
    }

    /// <summary>
    /// 包含表达式
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="parameterExpression">实例名</param>
    /// <param name="propertyName">属性名</param>
    /// <param name="value">属性值</param>
    /// <returns></returns>
    public static Expression<Func<T, bool>> GetStringContains<T>(this ParameterExpression parameterExpression, string propertyName, string value) where T : class
    {
        var prop = Expression.Property(parameterExpression, propertyName);
        var eq = typeof(string).GetMethod("Contains", new[] { typeof(string) })!;
        var constant = Expression.Constant(value, typeof(string));
        var containsExp = Expression.Call(prop, eq, constant);

        return Expression.Lambda<Func<T, bool>>(containsExp, parameterExpression);
    }

    /// <summary>
    /// 大于或等于表达式
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="parameterExpression">实例名</param>
    /// <param name="propertyName">属性名</param>
    /// <param name="value">属性值</param>
    /// <returns></returns>
    public static Expression<Func<T, bool>> GetGreaterThanOrEqual<T>(this ParameterExpression parameterExpression, string propertyName, object value) where T : class
    {
        return parameterExpression.GetYourDefine<T>(propertyName, value, Expression.GreaterThanOrEqual);
    }

    /// <summary>
    /// 大于表达式
    /// </summary>
    /// <param name="parameterExpression">实例名</param>
    /// <param name="propertyName">属性名</param>
    /// <param name="value">属性值</param>
    /// <returns></returns>
    public static Expression<Func<T, bool>> GetGreaterThan<T>(this ParameterExpression parameterExpression, string propertyName, object value) where T : class
    {
        return parameterExpression.GetYourDefine<T>(propertyName, value, Expression.GreaterThan);
    }

    /// <summary>
    /// 小于或等于表达式
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="parameterExpression">实例名</param>
    /// <param name="propertyName">属性名</param>
    /// <param name="value">属性值</param>
    /// <returns></returns>
    public static Expression<Func<T, bool>> GetLessThanOrEqual<T>(this ParameterExpression parameterExpression, string propertyName, object value) where T : class
    {
        return parameterExpression.GetYourDefine<T>(propertyName, value, Expression.LessThanOrEqual);
    }

    public static Expression<Func<T, bool>> GetEqual<T>(this ParameterExpression parameterExpression, string propertyName, object value) where T : class
    {
        return parameterExpression.GetYourDefine<T>(propertyName, value, Expression.Equal);
    }

    /// <summary>
    /// 小于表达式
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="parameterExpression">实例名</param>
    /// <param name="propertyName">属性名</param>
    /// <param name="value">属性值</param>
    /// <returns></returns>
    public static Expression<Func<T, bool>> GetLessThan<T>(this ParameterExpression parameterExpression, string propertyName, object value) where T : class
    {
        return parameterExpression.GetYourDefine<T>(propertyName, value, Expression.LessThan);
    }


    /// <summary>
    /// 传入你的表达式目录树方法,自动构建
    /// </summary>
    /// <typeparam name="T">类型</typeparam>
    /// <param name="parameterExpression">实例名</param>
    /// <param name="propertyName">属性名</param>
    /// <param name="value">属性值</param>
    /// <param name="func">构建方法</param>
    /// <returns></returns>
    public static Expression<Func<T, bool>> GetYourDefine<T>(this ParameterExpression parameterExpression, string propertyName, object value, Func<Expression, Expression, Expression> func) where T : class
    {
        var prop = Expression.Property(parameterExpression, propertyName);
        var constant = Expression.Constant(value, value.GetType());
        return Expression.Lambda<Func<T, bool>>(func(prop, constant), parameterExpression);
    }
}




/// <summary>
/// 表达式替换帮助类
/// </summary>
class ExpressionReplace : ExpressionVisitor
{
    /// <summary>
    /// 当前的表达式参数
    /// </summary>
    public ParameterExpression ParameterExpression { get; }

    /// <summary>
    /// ctor
    /// </summary>
    /// <param name="parameterExpression">表达式参数</param>
    public ExpressionReplace(ParameterExpression parameterExpression)
    {
        ParameterExpression = parameterExpression;
    }

    /// <summary>
    /// 替换成相同的引用
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    public Expression Replace(Expression expression)
    {
        return Visit(expression);
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        return ParameterExpression;
    }
}
