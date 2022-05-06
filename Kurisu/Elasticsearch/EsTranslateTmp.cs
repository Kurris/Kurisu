using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Kurisu.Elasticsearch
{
    public class EsTranslateTmp : ExpressionVisitor
    {
        private readonly Stack<string> _resultStack = new();
        private readonly Stack<string> _memberAccessNames = new();
        private readonly Stack<Type> _memberAccessTypes = new();

        public object GetQuery(Expression node)
        {
            this.Visit(node);

            _resultStack.Clear();
            return null;
        }

        public List<string> GetOrders(Expression node)
        {
            this.Visit(node);

            if (this._resultStack.Count == 0)
            {
                return new List<string>(0);
            }

            var elements = _resultStack.Select(x=>x.ToLower()).ToList();
            elements.Reverse();
            _resultStack.Clear();
            return elements;
        }


        protected override Expression VisitBinary(BinaryExpression node)
        {
            _resultStack.Push("}");
            base.Visit(node.Right);
            _resultStack.Push(ConditionType(node.NodeType));
            base.Visit(node.Left);
            _resultStack.Push("{");
            return node;
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            if (node.NodeType == ExpressionType.Convert)
            {
                //不做处理
            }

            return this.VisitMember(node.Operand as MemberExpression);
        }



        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Expression is ParameterExpression)
            {
                _resultStack.Push(node.Member.Name);
                return node;
            }


            if (node.Expression is ConstantExpression expression)
            {
                this._memberAccessNames.Push(node.Member.Name);
                return this.VisitConstant(expression);
            }

            _memberAccessNames.Push(node.Member.Name);
            _memberAccessTypes.Push(node.Type);

            if (node.Expression is MemberExpression memberExpression)
            {
                return this.VisitMember(memberExpression);
            }
            return base.VisitMember(node);
        }


        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (node.Type.IsClass && _memberAccessNames.Count > 0)
            {
                var memberName = _memberAccessNames.Pop();
                var value = node.Type.GetField(memberName).GetValue(node.Value);
                var t = GetValueType(value);
                if (t == 0)
                {
                    this._resultStack.Push(value.ToString());
                }
                else
                {
                    do
                    {
                        memberName = _memberAccessNames.Pop();
                        var type = this._memberAccessTypes.Pop();


                        value = this.GetType().GetMethod(nameof(AccessValue), BindingFlags.NonPublic | BindingFlags.Static)
                       .MakeGenericMethod(value.GetType(), type).Invoke(null, new[] { value, memberName });

                        if (_memberAccessNames.Count == 0)
                        {
                            if (value.GetType().IsEnum)
                            {
                                value = (int)value;
                            }

                            _resultStack.Push(value.ToString());
                        }

                    } while (_memberAccessNames.Count > 0);
                }

                return node;
            }


            this._resultStack.Push(node.Value.ToString());
            return node;
        }


        private static TP AccessValue<T, TP>(T t, string p)
        {
            var e = Expression.Parameter(t.GetType(), "e");
            var property = Expression.Property(e, p);
            var lambda = Expression.Lambda<Func<T, TP>>(property, e);
            return lambda.Compile()(t);
        }


        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            string name = node.Method.Name;

            var methodName = name switch
            {
                "Equals" => "=",

                _ => throw new NotImplementedException(),
            };

            this.Visit(node.Object);
            this.Visit(node.Arguments[0]);
            string right = this._resultStack.Pop();
            string left = this._resultStack.Pop();
            this._resultStack.Push(left + " " + methodName + " " + right);

            return node;
        }


        private int GetValueType(object o)
        {
            var t = o.GetType().Name;
            if (t == "Int32"
                || t == "String")
            {
                return 0;
            }

            return -1;
        }


        private static string ConditionType(ExpressionType expressionType)
        {
            return expressionType switch
            {
                ExpressionType.And => " and ",
                ExpressionType.AndAlso => " and ",
                ExpressionType.Equal => "=",
                ExpressionType.GreaterThan => " > ",
                ExpressionType.GreaterThanOrEqual => " >= ",
                ExpressionType.LessThan => " < ",
                ExpressionType.LessThanOrEqual => " <= ",
                ExpressionType.Not => "!=",
                ExpressionType.NotEqual => " != ",
                ExpressionType.Or => " or ",
                ExpressionType.OrElse => " or ",
            };
        }
    }
}
