using Kurisu.EFSharding.Core.VirtualRoutes.TableRoutes.RoutingRuleEngine;
using Kurisu.EFSharding.Sharding.MergeEngines.Common.Abstractions;
using Kurisu.EFSharding.Sharding.MergeEngines.Enumerables.Base;

namespace Kurisu.EFSharding.Sharding.MergeEngines.Common;

internal class SqlSequenceRouteUnit: ISqlRouteUnit
{
    public SequenceResult SequenceResult { get; }

    public SqlSequenceRouteUnit(SequenceResult sequenceResult)
    {
        SequenceResult = sequenceResult;
    }

    public string DatasourceName => SequenceResult.DSName;
    public TableRouteResult TableRouteResult => SequenceResult.TableRouteResult;
}