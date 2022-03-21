using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Kurisu.Elasticsearch.LowLinq.Syntax;

namespace Kurisu.Elasticsearch
{
    internal class EsTranslate : ExpressionVisitor
    {
        private readonly Stack<object> _resultStack = new();
        private readonly Stack<string> _memberAccessNames = new();
        private readonly Stack<Type> _memberAccessTypes = new();


        private readonly Dictionary<int, List<EsItem>> _deepItem = new();
        private int _deep;
        private readonly Dictionary<int, List<EsItem>> _top = new();
        private readonly Stack<int> _setIndexRecords = new();


        private EsItem _item;
        private EsConditionType _lastConditionType;


        public EsItem GetQuery(Expression node)
        {
            this.Visit(node);

            if (_deepItem.SelectMany(x => x.Value).Any())
            {
                var @bool = new EsItem(new Dictionary<string, object>
                {
                    ["must"] = _deepItem.First().Value
                });

                _item = new EsItem(new Dictionary<string, object>
                {
                    ["bool"] = @bool
                });           
            }

            _deepItem.Clear();
            if (_top.Count>0)
            {
                var tops = _top.ToDictionary(x => x.Key, x => x.Value);

                var lis = tops.SelectMany(x => x.Value);

                //var rules = _searchRules.Distinct();
                //var key = rules.All(x => x == EsConditionType.Should) || rules.Count() > 1 ? "should" : "must";
                _item = new EsItem(new Dictionary<string, object>
                {
                    ["bool"] = new EsItem(new Dictionary<string, object>()
                    {
                        [_lastConditionType.ToString().ToLower()] = lis
                    })
                });
            }
           

            return _item;
        }

        private bool _isOrder;
        private bool _isMethodCall;

        public List<string> GetOrders(Expression node)
        {
            _isOrder = true;
            this.Visit(node);

            if (this._resultStack.Count == 0)
            {
                return new List<string>(0);
            }

            var elements = _resultStack.Select(x => x.ToString().ToLower()).ToList();
            elements.Reverse();
            _resultStack.Clear();
            _isOrder = false;
            return elements;
        }

        private int _preCombineDeep;


        protected override Expression VisitBinary(BinaryExpression node)
        {
            _deep++;
            _deepItem.TryAdd(_deep, new List<EsItem>());
            base.Visit(node.Right);
            var nodeType = ConditionType(node.NodeType);

            _deepItem.TryGetValue(_deep, out var mustOrShould);
            if (_resultStack.Count > 0)
            {
                var item = _resultStack.Pop();
                if (item is EsItem esItem)
                {
                    mustOrShould.Add(esItem);
                    _setIndexRecords.Push(_deep);
                }
                else
                {
                    _resultStack.Push(item);
                    _resultStack.Push(nodeType);
                }
            }

            base.Visit(node.Left);
            if (_resultStack.Count > 0)
            {
                var esItem1 = _resultStack.Pop() as EsItem;
                mustOrShould.Add(esItem1);
                _setIndexRecords.Push(_deep);
            }

           
            if (nodeType is EsConditionType.Must or EsConditionType.Should)
            {
                bool hasSetIndexRecords = false;
                if (_setIndexRecords.Count > 0)
                {
                    hasSetIndexRecords = true;

                    var current = _setIndexRecords.Pop();
                    if (_setIndexRecords.Count > 0)
                    {
                        var next = _setIndexRecords.Pop();
                        while (next == current)
                        {
                            if (_setIndexRecords.Count > 0)
                            {
                                next = _setIndexRecords.Pop();
                            }
                            else
                            {
                                break;
                            }
                        }

                        _setIndexRecords.Push(next);
                    }


                    _deepItem.TryGetValue(current, out mustOrShould);
                    _deepItem.Remove(current);
                    _deepItem.TryAdd(current, new List<EsItem>());
                }

                var key = "must";
                if (nodeType == EsConditionType.Should)
                {
                    key = "should";
                }
                _lastConditionType = nodeType;

                if (!hasSetIndexRecords)
                {
                    var tops = _top.ToDictionary(x => x.Key, x => x.Value);

                    var elementes = tops.SelectMany(x => x.Value);
                 
                    _item = new EsItem(new Dictionary<string, object>
                    {
                        ["bool"] = new EsItem(new Dictionary<string, object>()
                        {
                            [key] = elementes
                        })
                    });

                    _top.Clear();

                    _deep--;
                    return node;
                }

                var @bool = new EsItem(new Dictionary<string, object>
                {
                    [key] = mustOrShould
                });

                var top = new EsItem(new Dictionary<string, object>
                {
                    ["bool"] = @bool
                });

                _top.TryAdd(_deep, new List<EsItem>());
                _top.TryGetValue(_deep, out var lis);
                lis.Add(top);

                if (_preCombineDeep == _deep || _deep==1)
                {
                    _top.TryAdd(_deep - 1, new List<EsItem>());
                    _top.TryGetValue(_deep - 1, out var lis1);

                    @bool = new EsItem(new Dictionary<string, object>
                    {
                        [key] = lis
                    });

                    top = new EsItem(new Dictionary<string, object>
                    {
                        ["bool"] = @bool
                    });
                    lis1.Add(top);

                    _top.Remove(_deep);
                }

                _preCombineDeep = _deep;

            }

            _deep--;
            return node;
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            if (node.NodeType == ExpressionType.Convert)
            {
                //这里好复杂,不做处理
            }

            return this.VisitMember((node.Operand as MemberExpression)!);
        }


        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Expression is ParameterExpression)
            {
                if (!_isOrder && !_isMethodCall)
                {
                    var conditionType = (EsConditionType)_resultStack.Pop();
                    var value = _resultStack.Pop();
                    var item = EsCondition.GetItem(node.Member.Name, value, conditionType);
                    _resultStack.Push(item);
                }
                else
                {
                    _resultStack.Push(node.Member.Name);
                }

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
                    _resultStack.Push(value);
                }
                else
                {
                    do
                    {
                        memberName = _memberAccessNames.Pop();
                        var type = this._memberAccessTypes.Pop();

                        value = this.GetType().GetMethod(nameof(EsTranslate.AccessValue), BindingFlags.NonPublic | BindingFlags.Static)
                            .MakeGenericMethod(value.GetType(), type).Invoke(null, new[] { value, memberName });

                        if (_memberAccessNames.Count == 0)
                        {
                            if (value.GetType().IsEnum)
                            {
                                value = (int)value;
                            }

                            _resultStack.Push(value);
                        }
                    } while (_memberAccessNames.Count > 0);
                }

                return node;
            }

            this._resultStack.Push(node.Value);
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
            _deep++;
            _isMethodCall = true;

            var name = node.Method.Name;

            var conditionType = name switch
            {
                "Equals" => EsConditionType.Term,
                "Contains" => EsConditionType.Match,
                _ => throw new NotImplementedException(),
            };

            this.Visit(node.Object);
            this.Visit(node.Arguments[0]);
            var right = this._resultStack.Pop().ToString();
            var left = this._resultStack.Pop().ToString();

            var item = EsCondition.GetItem(left, right, conditionType);

            _deepItem.TryAdd(_deep, new List<EsItem>());
            _deepItem.TryGetValue(_deep, out var mustOrShould);
            mustOrShould.Add(item);
            _setIndexRecords.Push(_deep);

            _isMethodCall = false;
            _deep--;
            return node;
        }


        private int GetValueType(object o)
        {
            var t = o.GetType().Name;
            if (t is "Int32" or "String")
            {
                return 0;
            }

            return -1;
        }


        private static EsConditionType ConditionType(ExpressionType expressionType)
        {
            return expressionType switch
            {
                ExpressionType.And => EsConditionType.Must,
                ExpressionType.AndAlso => EsConditionType.Must,
                ExpressionType.Equal => EsConditionType.Term,
                ExpressionType.Or => EsConditionType.Should,
                ExpressionType.OrElse => EsConditionType.Should,
            };
        }
    }
}