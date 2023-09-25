using System.Collections;
using Kurisu.EFSharding.Core.Internal.PriorityQueues;
using Kurisu.EFSharding.Exceptions;
using Kurisu.EFSharding.Sharding.Enumerators.AggregateExtensions;
using Kurisu.EFSharding.Sharding.Visitors.Selects;
using Kurisu.EFSharding.Extensions;

namespace Kurisu.EFSharding.Sharding.Enumerators.StreamMergeAsync;

internal class MultiAggregateOrderStreamMergeAsyncEnumerator<T> : IStreamMergeAsyncEnumerator<T>
{
    private readonly StreamMergeContext _mergeContext;
    private readonly IEnumerable<IStreamMergeAsyncEnumerator<T>> _enumerators;
    private readonly Core.Internal.PriorityQueues.PriorityQueue<IOrderStreamMergeAsyncEnumerator<T>> _queue;
    private T CurrentValue;
    private List<object> CurrentGroupValues;
    private bool _skipFirst;

    public MultiAggregateOrderStreamMergeAsyncEnumerator(StreamMergeContext mergeContext, IEnumerable<IStreamMergeAsyncEnumerator<T>> enumerators)
    {
        _mergeContext = mergeContext;
        _enumerators = enumerators;
        _queue = new PriorityQueue<IOrderStreamMergeAsyncEnumerator<T>>(enumerators.Count());
        _skipFirst = true;
        SetOrderEnumerator();
    }

    private void SetOrderEnumerator()
    {
        foreach (var source in _enumerators)
        {
            var orderStreamEnumerator = new OrderStreamMergeAsyncEnumerator<T>(_mergeContext, source);
            if (orderStreamEnumerator.HasElement())
            {
                orderStreamEnumerator.SkipFirst();
                _queue.Offer(orderStreamEnumerator);
            }
        }

        //设置第一个元素聚合的属性值
        CurrentGroupValues = _queue.IsEmpty() ? new List<object>(0) : GetCurrentGroupValues(_queue.Peek());
    }

    private List<object> GetCurrentGroupValues(IOrderStreamMergeAsyncEnumerator<T> enumerator)
    {
        var first = enumerator.ReallyCurrent;
        return _mergeContext.SelectContext.SelectProperties.Where(o => !(o is SelectAggregateProperty))
            .Select(o => first.GetValueByExpression(o.PropertyName).value).ToList();
    }

    public async ValueTask<bool> MoveNextAsync()
    {
        if (_queue.IsEmpty())
            return false;

        var hasNext = await SetCurrentValueAsync();
        if (hasNext)
        {
            CurrentGroupValues = _queue.IsEmpty() ? new List<object>(0) : GetCurrentGroupValues(_queue.Peek());
        }

        return hasNext;
    }

    private bool EqualWithGroupValues()
    {
        var current = GetCurrentGroupValues(_queue.Peek());
        for (int i = 0; i < CurrentGroupValues.Count; i++)
        {
            if (!object.Equals(CurrentGroupValues[i], current[i]))
                return false;
        }

        return true;
    }

    private async ValueTask<bool> SetCurrentValueAsync()
    {
        CurrentValue = default;
        var currentValues = new List<T>();
        while (EqualWithGroupValues())
        {
            var current = _queue.Peek().GetCurrent();
            currentValues.Add(current);
            var first = _queue.Poll();

            if (await first.MoveNextAsync())
            {
                _queue.Offer(first);
            }

            if (_queue.IsEmpty())
            {
                break;
            }
        }

        MergeValue(currentValues);

        return true;
    }

    public bool MoveNext()
    {
        if (_queue.IsEmpty())
            return false;
        var hasNext = SetCurrentValue();
        if (hasNext)
        {
            CurrentGroupValues = _queue.IsEmpty() ? new List<object>(0) : GetCurrentGroupValues(_queue.Peek());
        }

        return hasNext;
    }

    private bool SetCurrentValue()
    {
        CurrentValue = default;
        var currentValues = new List<T>();
        while (EqualWithGroupValues())
        {
            var current = _queue.Peek().GetCurrent();
            currentValues.Add(current);
            var first = _queue.Poll();

            if (first.MoveNext())
            {
                _queue.Offer(first);
            }

            if (_queue.IsEmpty())
            {
                break;
            }
        }

        MergeValue(currentValues);

        return true;
    }

    private void MergeValue(List<T> aggregateValues)
    {
        if (aggregateValues.IsNotEmpty())
        {
            // var copyFields = string.Join(",", _mergeContext.SelectContext.SelectProperties.Select(o=>o.PropertyName));
            CurrentValue = AggregateExtension.CopyTSource(aggregateValues.First());

            if (aggregateValues.Count > 1)
            {
                var aggregates = _mergeContext.SelectContext.SelectProperties.OfType<SelectAggregateProperty>().ToList();
                if (aggregates.IsNotEmpty())
                {
                    var propertyValues = new LinkedList<(string Name, object Value)>();
                    foreach (var aggregate in aggregates)
                    {
                        object aggregateValue = null;
                        if (aggregate is SelectCountProperty || aggregate is SelectSumProperty)
                        {
                            aggregateValue = aggregateValues.AsQueryable().SumByProperty(aggregate.Property);
                        }
                        else if (aggregate is SelectMaxProperty)
                        {
                            aggregateValue = aggregateValues.AsQueryable().Max(aggregate.Property);
                        }
                        else if (aggregate is SelectMinProperty)
                        {
                            aggregateValue = aggregateValues.AsQueryable().Min(aggregate.Property);
                        }
                        else if (aggregate is SelectAverageProperty selectAverageProperty)
                        {
                            if (selectAverageProperty.CountProperty != null)
                            {
                                aggregateValue = aggregateValues.AsQueryable().AverageWithCount(selectAverageProperty.Property, selectAverageProperty.CountProperty, selectAverageProperty.Property.PropertyType);
                            }
                            else if (selectAverageProperty.SumProperty != null)
                            {
                                aggregateValue = aggregateValues.AsQueryable().AverageWithSum(selectAverageProperty.Property, selectAverageProperty.SumProperty, selectAverageProperty.Property.PropertyType);
                            }
                            else
                            {
                                throw new ShardingCoreInvalidOperationException($"method:{aggregate.AggregateMethod} invalid operation ");
                            }
                        }
                        else
                        {
                            throw new ShardingCoreInvalidOperationException($"method:{aggregate.AggregateMethod} invalid operation ");
                        }

                        propertyValues.AddLast((Name: aggregate.PropertyName, Value: aggregateValue));
                    }

                    foreach (var propertyValue in propertyValues)
                    {
                        CurrentValue.SetPropertyValue(propertyValue.Name, propertyValue.Value);
                    }
                }
            }
        }
    }


    public bool SkipFirst()
    {
        return true;
    }

    public bool HasElement()
    {
        return ReallyCurrent != null;
    }

    public T ReallyCurrent => _queue.IsEmpty() ? default(T) : _queue.Peek().ReallyCurrent;

    public T GetCurrent()
    {
        return CurrentValue;
    }

#if !EFCORE2

    public async ValueTask DisposeAsync()
    {
        foreach (var enumerator in _enumerators)
        {
            await enumerator.DisposeAsync();
        }
    }
#endif


    public void Reset()
    {
        throw new NotImplementedException();
    }

    object IEnumerator.Current => Current;

    public T Current => CurrentValue;

    public void Dispose()
    {
        foreach (var enumerator in _enumerators)
        {
            enumerator.Dispose();
        }
    }
}