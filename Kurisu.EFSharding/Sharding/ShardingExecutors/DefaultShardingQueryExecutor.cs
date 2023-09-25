using System.Linq.Expressions;
using Kurisu.EFSharding.Exceptions;
using Kurisu.EFSharding.Sharding.Abstractions;
using Kurisu.EFSharding.Sharding.MergeEngines.EnumeratorStreamMergeEngines;
using Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines;
using Kurisu.EFSharding.Sharding.MergeEngines.ShardingMergeEngines.Abstractions.InMemoryMerge;
using Kurisu.EFSharding.Sharding.ShardingExecutors.Abstractions;
using Kurisu.EFSharding.Extensions;

namespace Kurisu.EFSharding.Sharding.ShardingExecutors;

internal class DefaultShardingQueryExecutor : IShardingQueryExecutor
{
    private readonly IStreamMergeContextFactory _streamMergeContextFactory;


    public DefaultShardingQueryExecutor(IStreamMergeContextFactory streamMergeContextFactory)
    {
        _streamMergeContextFactory = streamMergeContextFactory;
    }

    public TResult Execute<TResult>(IMergeQueryCompilerContext mergeQueryCompilerContext)
    {
        //如果根表达式为tolist toarray getenumerator等表示需要迭代
        return mergeQueryCompilerContext.IsEnumerableQuery()
            ? EnumerableExecute<TResult>(mergeQueryCompilerContext)
            : DoExecute<TResult>(mergeQueryCompilerContext, false);
    }


    public TResult ExecuteAsync<TResult>(IMergeQueryCompilerContext mergeQueryCompilerContext,
        CancellationToken cancellationToken = new())
    {
        if (mergeQueryCompilerContext.IsEnumerableQuery())
        {
            return EnumerableExecute<TResult>(mergeQueryCompilerContext);
        }

        if (typeof(TResult).HasImplementedRawGeneric(typeof(Task<>)))
        {
            return DoExecute<TResult>(mergeQueryCompilerContext, true, cancellationToken);
        }


        throw new ShardingCoreException($"db context operator not support query expression:[{mergeQueryCompilerContext.GetQueryExpression().ShardingPrint()}] result type:[{typeof(TResult).FullName}]");
    }

    private TResult DoExecute<TResult>(IMergeQueryCompilerContext mergeQueryCompilerContext, bool async,
        CancellationToken cancellationToken = new())
    {
        var queryMethodName = mergeQueryCompilerContext.GetQueryMethodName();
        switch (queryMethodName)
        {
            case nameof(Enumerable.First):
                return EnsureResultTypeMergeExecute<TResult>(typeof(FirstSkipAsyncInMemoryMergeEngine<>),
                    mergeQueryCompilerContext, async, cancellationToken);
            case nameof(Enumerable.FirstOrDefault):
                return EnsureResultTypeMergeExecute<TResult>(typeof(FirstOrDefaultSkipAsyncInMemoryMergeEngine<>),
                    mergeQueryCompilerContext, async, cancellationToken);
            case nameof(Enumerable.Last):
                return EnsureResultTypeMergeExecute<TResult>(typeof(LastSkipAsyncInMemoryMergeEngine<>),
                    mergeQueryCompilerContext, async, cancellationToken);
            case nameof(Enumerable.LastOrDefault):
                return EnsureResultTypeMergeExecute<TResult>(typeof(LastOrDefaultSkipAsyncInMemoryMergeEngine<>),
                    mergeQueryCompilerContext, async, cancellationToken);
            case nameof(Enumerable.Single):
                return EnsureResultTypeMergeExecute<TResult>(typeof(SingleSkipAsyncInMemoryMergeEngine<>),
                    mergeQueryCompilerContext, async, cancellationToken);
            case nameof(Enumerable.SingleOrDefault):
                return EnsureResultTypeMergeExecute<TResult>(typeof(SingleOrDefaultSkipAsyncInMemoryMergeEngine<>),
                    mergeQueryCompilerContext, async, cancellationToken);
            case nameof(Enumerable.Count):
                return EnsureResultTypeMergeExecute<TResult>(typeof(CountAsyncInMemoryMergeEngine<>),
                    mergeQueryCompilerContext, async, cancellationToken);
            case nameof(Enumerable.LongCount):
                return EnsureResultTypeMergeExecute<TResult>(typeof(LongCountAsyncInMemoryMergeEngine<>),
                    mergeQueryCompilerContext, async, cancellationToken);
            case nameof(Enumerable.Any):
                return EnsureResultTypeMergeExecute<TResult>(typeof(AnyAsyncInMemoryMergeEngine<>),
                    mergeQueryCompilerContext, async, cancellationToken);
            case nameof(Enumerable.All):
                return EnsureResultTypeMergeExecute<TResult>(typeof(AllAsyncInMemoryMergeEngine<>),
                    mergeQueryCompilerContext, async, cancellationToken);
            case nameof(Enumerable.Max):
                return EnsureResultTypeMergeExecute2<TResult>(typeof(MaxAsyncInMemoryMergeEngine<,>),
                    mergeQueryCompilerContext, async, cancellationToken);
            case nameof(Enumerable.Min):
                return EnsureResultTypeMergeExecute2<TResult>(typeof(MinAsyncInMemoryMergeEngine<,>),
                    mergeQueryCompilerContext, async, cancellationToken);
            case nameof(Enumerable.Sum):
                return EnsureResultTypeMergeExecute2<TResult>(typeof(SumAsyncInMemoryMergeEngine<,>),
                    mergeQueryCompilerContext, async, cancellationToken);
            case nameof(Enumerable.Average):
                return EnsureResultTypeMergeExecute3<TResult>(typeof(AverageAsyncInMemoryMergeEngine<,,>),
                    mergeQueryCompilerContext, async, cancellationToken);
            case nameof(Enumerable.Contains):
                return EnsureResultTypeMergeExecute<TResult>(typeof(ContainsAsyncInMemoryMergeEngine<>),
                    mergeQueryCompilerContext, async, cancellationToken);

#if EFCORE7
                case nameof(RelationalQueryableExtensions.ExecuteUpdate):
                    return EnsureResultTypeMergeExecute<TResult>(typeof(ExecuteUpdateAsyncMemoryMergeEngine<>),
                        mergeQueryCompilerContext, async, cancellationToken);
                case nameof(RelationalQueryableExtensions.ExecuteDelete):
                    return EnsureResultTypeMergeExecute<TResult>(typeof(ExecuteDeleteAsyncMemoryMergeEngine<>),
                        mergeQueryCompilerContext, async, cancellationToken);
#endif
        }


        throw new ShardingCoreException(
            $"db context operator not support query expression:[{mergeQueryCompilerContext.GetQueryExpression().ShardingPrint()}]  result type:[{typeof(TResult).FullName}]");
    }

    private StreamMergeContext GetStreamMergeContext(IMergeQueryCompilerContext mergeQueryCompilerContext)
    {
        return _streamMergeContextFactory.Create(mergeQueryCompilerContext);
    }

    private TResult EnumerableExecute<TResult>(IMergeQueryCompilerContext mergeQueryCompilerContext)
    {
        var combineQueryable = mergeQueryCompilerContext.GetQueryCombineResult().GetCombineQueryable();
        var queryEntityType = combineQueryable.ElementType;
        var streamMergeContext = GetStreamMergeContext(mergeQueryCompilerContext);

        Type streamMergeEngineType = typeof(AsyncEnumeratorStreamMergeEngine<>);
        streamMergeEngineType = streamMergeEngineType.MakeGenericType(queryEntityType);
        return (TResult) Activator.CreateInstance(streamMergeEngineType, streamMergeContext);
    }

    private TResult EnsureResultTypeMergeExecute<TResult>(Type streamMergeEngineType,
        IMergeQueryCompilerContext mergeQueryCompilerContext, bool async, CancellationToken cancellationToken)
    {
        var combineQueryable = mergeQueryCompilerContext.GetQueryCombineResult().GetCombineQueryable();
        var queryEntityType = combineQueryable.ElementType;
        var newStreamMergeEngineType = streamMergeEngineType.MakeGenericType(queryEntityType);
        var streamMergeContext = GetStreamMergeContext(mergeQueryCompilerContext);
        var streamEngine = Activator.CreateInstance(newStreamMergeEngineType, streamMergeContext);
        var methodName = async
            ? nameof(IEnsureMergeResult<object>.MergeResultAsync)
            : nameof(IEnsureMergeResult<object>.MergeResult);
        var streamEngineMethod = newStreamMergeEngineType.GetMethod(methodName);
        if (streamEngineMethod == null)
            throw new ShardingCoreException($"cant found InMemoryAsyncStreamMergeEngine method [{methodName}]");
        var @params = async ? new object[] {cancellationToken} : Array.Empty<object>();
        return (TResult) streamEngineMethod.Invoke(streamEngine, @params);
    }

    private TResult EnsureResultTypeMergeExecute2<TResult>(Type streamMergeEngineType,
        IMergeQueryCompilerContext mergeQueryCompilerContext, bool async, CancellationToken cancellationToken)
    {
        var streamMergeContext = GetStreamMergeContext(mergeQueryCompilerContext);
        var resultType = (mergeQueryCompilerContext.GetQueryExpression() as MethodCallExpression).GetResultType();
        streamMergeEngineType = streamMergeEngineType.MakeGenericType(resultType, resultType);
        var streamEngine = Activator.CreateInstance(streamMergeEngineType, streamMergeContext);
        var methodName = async
            ? nameof(IEnsureMergeResult<object>.MergeResultAsync)
            : nameof(IEnsureMergeResult<object>.MergeResult);
        var streamEngineMethod = streamMergeEngineType.GetMethod(methodName);
        if (streamEngineMethod == null)
            throw new ShardingCoreException($"cant found InMemoryAsyncStreamMergeEngine method [{methodName}]");
        var @params = async ? new object[] {cancellationToken} : Array.Empty<object>();
        return (TResult) streamEngineMethod.Invoke(streamEngine, @params);
    }

    private TResult EnsureResultTypeMergeExecute3<TResult>(Type streamMergeEngineType,
        IMergeQueryCompilerContext mergeQueryCompilerContext, bool async, CancellationToken cancellationToken)
    {
        var combineQueryable = mergeQueryCompilerContext.GetQueryCombineResult().GetCombineQueryable();
        var queryEntityType = combineQueryable.ElementType;
        var resultType = (mergeQueryCompilerContext.GetQueryExpression() as MethodCallExpression).GetResultType();

        if (async)
            streamMergeEngineType = streamMergeEngineType.MakeGenericType(queryEntityType,
                typeof(TResult).GetGenericArguments()[0], resultType);
        else
            streamMergeEngineType =
                streamMergeEngineType.MakeGenericType(queryEntityType, typeof(TResult), resultType);

        var streamMergeContext = GetStreamMergeContext(mergeQueryCompilerContext);
        var streamEngine = Activator.CreateInstance(streamMergeEngineType, streamMergeContext);
        var methodName = async
            ? nameof(IEnsureMergeResult<object>.MergeResultAsync)
            : nameof(IEnsureMergeResult<object>.MergeResult);
        var streamEngineMethod = streamMergeEngineType.GetMethod(methodName);
        if (streamEngineMethod == null)
            throw new ShardingCoreException($"cant found InMemoryAsyncStreamMergeEngine method [{methodName}]");
        var @params = async ? new object[] {cancellationToken} : new object[0];
        return (TResult) streamEngineMethod.Invoke(streamEngine, @params);
    }
}