using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Kurisu.DataAccessor.Helpers
{
    /// <summary>
    /// 表达式目录树Visitor
    /// </summary>
    internal class ConditionBuilderVisitor : ExpressionVisitor
    {
        internal ConditionBuilderVisitor(string sqlType)
        {
            _filedTagLeft = sqlType switch
            {
                "SqlServer" => " [",
                "MySql" => " `",
                _ => throw new NotSupportedException(sqlType)
            };

            _filedTagRight = sqlType switch
            {
                "SqlServer" => "] ",
                "MySql" => "` ",
                _ => throw new NotSupportedException(sqlType)
            };
        }

        private readonly string _filedTagLeft = string.Empty;
        private readonly string _filedTagRight = string.Empty;

        private readonly Stack<string> _stringStack = new Stack<string>();

        /// <summary>
        /// 组合
        /// </summary>
        /// <returns>表达式</returns>
        public string Combine()
        {
            if (this._stringStack.Count == 0) throw new ArgumentException("表达式不存在");

            string condition = string.Concat(this._stringStack);
            this._stringStack.Clear();
            return condition;
        }


        /// <summary>
        /// 组合,带上Where 1=1 and
        /// </summary>
        /// <returns>条件表达式</returns>
        public string CombineWithWhere()
        {
            if (this._stringStack.Count == 0) throw new ArgumentException("表达式不存在");

            string condition = string.Concat(this._stringStack);
            this._stringStack.Clear();
            return " WHERE 1=1 and " + condition;
        }

        public override Expression Visit(Expression node)
        {
            return base.Visit(node);
        }

        /// <summary>
        /// 二元表达式
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitBinary(BinaryExpression node)
        {
            this._stringStack.Push(")");
            base.Visit(node.Right);
            this._stringStack.Push(" " + ConditionType(node.NodeType) + " ");
            base.Visit(node.Left);
            this._stringStack.Push("(");
            return node;
        }


        /// <summary>
        /// 访问成员
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Expression is ParameterExpression)
            {
                this._stringStack.Push($" {_filedTagLeft}" + node.Member.Name + $"{_filedTagRight} ");
                return node;
            }
            else if (node.Expression is ConstantExpression)
            {
                return this.VisitConstant(node.Expression as ConstantExpression);
            }
            else
            {
                this._stringStack.Push($" {_filedTagLeft}" + node.Member.Name + $"{_filedTagRight} ");
                return node;
            }
        }


        /// <summary>
        /// 访问常量
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (node.Type.Name.Contains("<>") && node.Type.Name.Contains("DisplayClass"))
            {
                var field = node.Type.GetFields()[0];

                if (field.FieldType.IsEnum)
                {
                    this._stringStack.Push(" '" + (int) field.GetValue(node.Value) + $"' ");
                }
                else
                {
                    this._stringStack.Push(" '" + field.GetValue(node.Value) + $"' ");
                }
            }
            else
            {
                if (bool.TryParse(node.Value + "", out _))
                {
                    this._stringStack.Push(" 1=1 ");
                }
                else if (node.Value != null)
                {
                    var t = node.Value.GetType();
                    if (t.IsEnum)
                    {
                        this._stringStack.Push(" '" + (int) node.Value + $"' ");
                    }
                    else
                    {
                        this._stringStack.Push(" '" + node.Value + $"' ");
                    }
                }
                else
                {
                    this._stringStack.Push(" '" + node.Value + $"' ");
                }
            }

            return node;
        }


        /// <summary>
        /// 访问方法
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            string methodName = node.Method.Name;
            if (methodName.Equals("First"))
            {
                return node;
            }

            string format = methodName switch
            {
                "Contains" => "({0} LIKE '%'{1}'%')",
                "StartsWith" => "({0} LIKE {1}'%')",
                "EndsWith" => "({0} LIKE '%'{1})",
                "Equals" => "({0} = {1})",
                _ => throw new NotSupportedException(node.Method.Name),
            };
            this.Visit(node.Object);
            this.Visit(node.Arguments[0]);
            string right = this._stringStack.Pop();
            string left = this._stringStack.Pop();
            this._stringStack.Push(string.Format(format, left, right));

            return node;
        }


        /// <summary>
        /// 条件类型
        /// </summary>
        /// <param name="expressionType"></param>
        /// <returns></returns>
        public string ConditionType(ExpressionType expressionType)
        {
            switch (expressionType)
            {
                case ExpressionType.Add:
                    return "+";
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                    return "AND";
                case ExpressionType.Equal:
                    return "=";
                case ExpressionType.GreaterThan:
                    return ">";
                case ExpressionType.GreaterThanOrEqual:
                    return ">=";
                case ExpressionType.LessThan:
                    return "<";
                case ExpressionType.LessThanOrEqual:
                    return "<=";
                case ExpressionType.Multiply:
                    return "*";
                case ExpressionType.Not:
                    return "NOT";
                case ExpressionType.NotEqual:
                    return "<>";
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                    return "OR";
                case ExpressionType.Subtract:
                    return "-";
                default:
                    break;
            }

            throw new NotSupportedException();
        }
    }
}