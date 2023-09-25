namespace Kurisu.EFSharding.Core.VirtualRoutes.TableRoutes.RouteTails.Abstractions;

/// <summary>
/// 多模型比如join
/// </summary>
public interface IMultiQueryRouteTail : IRouteTail, INoCacheRouteTail
{
    /// <summary>
    /// 获取对象类型的应该后缀
    /// </summary>
    /// <param name="entityType"></param>
    /// <returns></returns>
    string GetEntityTail(Type entityType);

    ISet<Type> GetEntityTypes();
}