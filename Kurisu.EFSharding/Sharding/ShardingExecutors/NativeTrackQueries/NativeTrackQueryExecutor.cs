using Kurisu.EFSharding.Core.QueryTrackers;
using Kurisu.EFSharding.Core.TrackerManagers;
using Kurisu.EFSharding.Sharding.Enumerators.TrackerEnumerables;
using Kurisu.EFSharding.Sharding.ShardingExecutors.Abstractions;

namespace Kurisu.EFSharding.Sharding.ShardingExecutors.NativeTrackQueries;

public class NativeTrackQueryExecutor : INativeTrackQueryExecutor
{
    private readonly IQueryTracker _queryTracker;
    private readonly ITrackerManager _trackerManager;

    public NativeTrackQueryExecutor(IQueryTracker queryTracker,ITrackerManager trackerManager)
    {
        _queryTracker = queryTracker;
        _trackerManager = trackerManager;
    }
    public TResult Track<TResult>(IQueryCompilerContext queryCompilerContext, TResult resultTask)
    {

        if (resultTask != null)
        {

            if (_trackerManager.EntityUseTrack(resultTask.GetType()))
            {
                var trackedEntity = _queryTracker.Track(resultTask, queryCompilerContext.GetShardingDbContext());
                if (trackedEntity != null)
                {
                    return (TResult)trackedEntity;
                }
            }
        }
        return resultTask;
    }
    public async Task<TResult> TrackAsync<TResult>(IQueryCompilerContext queryCompilerContext, Task<TResult> resultTask)
    {
        var result = await resultTask;
        return Track(queryCompilerContext, result);
    }
    public IEnumerable<TResult> TrackEnumerable<TResult>(IQueryCompilerContext queryCompilerContext, IEnumerable<TResult> enumerable)
    {
        return new TrackEnumerable<TResult>(queryCompilerContext.GetShardingDbContext(), enumerable);
    }

    public IAsyncEnumerable<TResult> TrackAsyncEnumerable<TResult>(IQueryCompilerContext queryCompilerContext, IAsyncEnumerable<TResult> asyncEnumerable)
    {

        return new AsyncTrackerEnumerable<TResult>(queryCompilerContext.GetShardingDbContext(), asyncEnumerable);
    }

}