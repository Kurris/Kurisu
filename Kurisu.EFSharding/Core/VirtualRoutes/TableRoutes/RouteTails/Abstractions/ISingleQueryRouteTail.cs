namespace Kurisu.EFSharding.Core.VirtualRoutes.TableRoutes.RouteTails.Abstractions;

public interface ISingleQueryRouteTail : IRouteTail
{
    /// <summary>
    /// 获取当前查询的后缀
    /// </summary>
    /// <returns></returns>
    string GetTail();
}