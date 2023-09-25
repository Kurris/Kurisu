﻿namespace Kurisu.EFSharding.Sharding.MergeEngines.Executors.CircuitBreakers;

internal class ContainsCircuitBreaker:AbstractCircuitBreaker
{
    public ContainsCircuitBreaker(StreamMergeContext streamMergeContext) : base(streamMergeContext)
    {
    }

    protected override bool OrderConditionTerminated<TResult>(IEnumerable<TResult> results)
    {
        return results.Any(o => o is true);
    }

    protected override bool RandomConditionTerminated<TResult>(IEnumerable<TResult> results)
    {
        return results.Any(o => o is true);
    }
}