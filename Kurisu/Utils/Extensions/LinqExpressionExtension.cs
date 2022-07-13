using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Kurisu.Utils.Extensions
{
    public static class LinqExpressionExtension
    {
        /// <summary>
        /// 返回一个默认<see cref="true"/>的表达式
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <returns><see cref="Expression{Func{T, bool}}"/></returns>
        public static Expression<Func<T, bool>> True<T>()
        {
            return x => true;
        }

        /// <summary>
        /// 返回一个默认<see cref="false"/>的表达式
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
        /// <param name="predicateAddition">并且的表达式</param>
        /// <returns><see cref="Expression{Func{T, bool}}"/></returns>
        public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> predicateCurrent, Expression<Func<T, bool>> predicateAddition) where T : class
        {
            if (predicateCurrent == null) predicateCurrent = False<T>();
            return Combine(predicateCurrent, predicateAddition, Expression.AndAlso);
        }

        /// <summary>
        /// 或者
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="predicateCurrent">当前表达式</param>
        /// <param name="predicateAddition">并且的表达式</param>
        /// <returns><see cref="Expression{Func{T, bool}}"/></returns>
        public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> predicateCurrent, Expression<Func<T, bool>> predicateAddition) where T : class
        {
            if (predicateCurrent == null) predicateCurrent = False<T>();
            return Combine(predicateCurrent, predicateAddition, Expression.OrElse);
        }

        /// <summary>
        /// 否
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="predicateCurrent">当前表达式</param>
        /// <returns><see cref="Expression{Func{T, bool}}"/></returns>
        public static Expression<Func<T, bool>> Not<T>(this Expression<Func<T, bool>> predicateCurrent) where T : class
        {
            ExpressionReplace ex = GenExpressionReplace<T>(out var expressionPara);
            var current = ex.Replace(predicateCurrent);
            var body = Expression.Not(current);

            return Expression.Lambda<Func<T, bool>>(body, expressionPara);
        }

        /// <summary>
        /// 合并俩个表达式
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="expressionFirst">第一个表达式</param>
        /// <param name="expressionSecond">第二个表达式</param>
        /// <param name="expressionMethod">合并方法</param>
        /// <returns><see cref="Expression{Func{T, bool}}"/></returns>
        private static Expression<Func<T, bool>> Combine<T>(Expression<Func<T, bool>> expressionFirst, Expression<Func<T, bool>> expressionSecond, Func<Expression, Expression, Expression> expressionMethod)
        {
            ExpressionReplace ex = GenExpressionReplace<T>(out var expressionPara);

            var left = ex.Replace(expressionFirst.Body);
            var right = ex.Replace(expressionSecond.Body);

            return Expression.Lambda<Func<T, bool>>(expressionMethod(left, right), expressionPara);
        }

        /// <summary>
        /// 表达式替换帮助类
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="expressionPara">引用相等的表达式参数</param>
        /// <returns><see cref="ExpressionReplace"/></returns>
        private static ExpressionReplace GenExpressionReplace<T>(out ParameterExpression expressionPara)
        {
            expressionPara = Expression.Parameter(typeof(T), "x");
            return new ExpressionReplace(expressionPara);
        }
    }

    /// <summary>
    /// 表达式替换帮助类
    /// </summary>
    internal class ExpressionReplace : ExpressionVisitor
    {
        /// <summary>
        /// 当前的表达式参数
        /// </summary>
        public ParameterExpression ParameterExpression { get; private set; }

        /// <summary>
        /// Ctor
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


    /// <summary>
    /// 表达式构建sql帮助类
    /// </summary>
    public static class ExpressionHelper
    {
        /// <summary>
        /// Ids 获取Or表达式
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="ids">id集合</param>
        /// <param name="func">表达式方法</param>
        /// <returns></returns>
        public static Expression<Func<T, bool>> GetOrExpression<T>(this IEnumerable<int> ids, Func<int, Expression<Func<T, bool>>> func) where T : class
        {
            Expression<Func<T, bool>> exps = LinqExpressionExtension.False<T>();
            foreach (var id in ids)
            {
                var exp = func.Invoke(id);
                exps = exps.Or(exp);
            }

            return exps;
        }

        public static Expression<Func<T, bool>> GetOrExpression<T>(this IEnumerable<string> ids, Func<string, Expression<Func<T, bool>>> func) where T : class
        {
            Expression<Func<T, bool>> exps = LinqExpressionExtension.False<T>();
            foreach (var id in ids)
            {
                var exp = func.Invoke(id);
                exps = exps.Or(exp);
            }

            return exps;
        }

        /// <summary>
        /// Ts 获取Or表达式
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="ts">实体集合</param>
        /// <param name="func">表达式方法</param>
        /// <returns></returns>
        public static Expression<Func<T, bool>> GetOrExpression<T>(this IEnumerable<T> ts, Func<T, Expression<Func<T, bool>>> func) where T : class
        {
            Expression<Func<T, bool>> exps = LinqExpressionExtension.False<T>();
            foreach (var t in ts)
            {
                var exp = func.Invoke(t);
                exps = exps.Or(exp);
            }

            return exps;
        }

        /// <summary>
        /// 获取实例的表达式
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <returns></returns>
        public static ParameterExpression GetParameter<T>() where T : class
        {
            return Expression.Parameter(typeof(T), typeof(T).Name);
        }

        /// <summary>
        /// 枚举比较表达式
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="parameterExpression">实例名</param>
        /// <param name="propertityName">属性名</param>
        /// <param name="value">属性值</param>
        /// <returns></returns>
        public static Expression<Func<T, bool>> GetEnumEquals<T>(this ParameterExpression parameterExpression, string propertityName, object value) where T : class
        {
            MemberExpression prop = Expression.Property(parameterExpression, propertityName);
            MethodInfo eq = value.GetType().GetMethods().FirstOrDefault(x => x.Name == "Equals");
            ConstantExpression constant = Expression.Constant(value, typeof(object));
            MethodCallExpression eqExp = Expression.Call(prop, eq, constant);

            return Expression.Lambda<Func<T, bool>>(eqExp, parameterExpression);
        }

        /// <summary>
        /// 包含表达式
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="parameterExpression">实例名</param>
        /// <param name="propertityName">属性名</param>
        /// <param name="value">属性值</param>
        /// <returns></returns>
        public static Expression<Func<T, bool>> GetStringContains<T>(this ParameterExpression parameterExpression, string propertityName, string value) where T : class
        {
            MemberExpression prop = Expression.Property(parameterExpression, propertityName);
            MethodInfo eq = typeof(string).GetMethod("Contains", new Type[] {typeof(string)});
            ConstantExpression constant = Expression.Constant(value, typeof(string));
            MethodCallExpression containsExp = Expression.Call(prop, eq, constant);

            return Expression.Lambda<Func<T, bool>>(containsExp, parameterExpression);
        }

        /// <summary>
        /// 大于或等于表达式
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="parameterExpression">实例名</param>
        /// <param name="propertityName">属性名</param>
        /// <param name="value">属性值</param>
        /// <returns></returns>
        public static Expression<Func<T, bool>> GetGreaterThanOrEqual<T>(this ParameterExpression parameterExpression, string propertityName, object value) where T : class
        {
            return parameterExpression.GetYourDefine<T>(propertityName, value, Expression.GreaterThanOrEqual);
        }

        /// <summary>
        /// 大于表达式
        /// </summary>
        /// <typeparam name="T"><类型/typeparam>
        /// <param name="parameterExpression">实例名</param>
        /// <param name="propertityName">属性名</param>
        /// <param name="value">属性值</param>
        /// <returns></returns>
        public static Expression<Func<T, bool>> GetGreaterThan<T>(this ParameterExpression parameterExpression, string propertityName, object value) where T : class
        {
            return parameterExpression.GetYourDefine<T>(propertityName, value, Expression.GreaterThan);
        }

        /// <summary>
        /// 小于或等于表达式
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="parameterExpression">实例名</param>
        /// <param name="propertityName">属性名</param>
        /// <param name="value">属性值</param>
        /// <returns></returns>
        public static Expression<Func<T, bool>> GetLessThanOrEqual<T>(this ParameterExpression parameterExpression, string propertityName, object value) where T : class
        {
            return parameterExpression.GetYourDefine<T>(propertityName, value, Expression.LessThanOrEqual);
        }

        public static Expression<Func<T, bool>> GetEqual<T>(this ParameterExpression parameterExpression, string propertityName, object value) where T : class
        {
            return parameterExpression.GetYourDefine<T>(propertityName, value, Expression.Equal);
        }

        /// <summary>
        /// 小于表达式
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="parameterExpression">实例名</param>
        /// <param name="propertityName">属性名</param>
        /// <param name="value">属性值</param>
        /// <returns></returns>
        public static Expression<Func<T, bool>> GetLessThan<T>(this ParameterExpression parameterExpression, string propertityName, object value) where T : class
        {
            return parameterExpression.GetYourDefine<T>(propertityName, value, Expression.LessThan);
        }


        /// <summary>
        /// 传入你的表达式目录树方法,自动构建
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="parameterExpression">实例名</param>
        /// <param name="propertityName">属性名</param>
        /// <param name="value">属性值</param>
        /// <param name="func">构建方法</param>
        /// <returns></returns>
        public static Expression<Func<T, bool>> GetYourDefine<T>(this ParameterExpression parameterExpression, string propertityName, object value, Func<Expression, Expression, Expression> func) where T : class
        {
            MemberExpression prop = Expression.Property(parameterExpression, propertityName);
            ConstantExpression constant = Expression.Constant(value, value.GetType());
            return Expression.Lambda<Func<T, bool>>(func(prop, constant), parameterExpression);
        }
    }
}