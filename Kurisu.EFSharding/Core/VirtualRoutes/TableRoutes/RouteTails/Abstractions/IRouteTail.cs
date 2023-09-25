namespace Kurisu.EFSharding.Core.VirtualRoutes.TableRoutes.RouteTails.Abstractions;

public interface IRouteTail
{
    string GetRouteTailIdentity();
    bool IsMultiEntityQuery();
    bool IsShardingTableQuery();
}