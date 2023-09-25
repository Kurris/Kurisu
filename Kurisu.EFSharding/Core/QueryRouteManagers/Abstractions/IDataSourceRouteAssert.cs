namespace Kurisu.EFSharding.Core.QueryRouteManagers.Abstractions;

/// <summary>
/// 路由断言
/// </summary>
public interface IDataSourceRouteAssert
{
    /// <summary>
    /// 断言路由结果
    /// </summary>
    /// <param name="allDataSources">所有的路由数据源</param>
    /// <param name="resultDataSources">本次查询路由返回结果</param>
    void Assert(List<string> allDataSources, List<string> resultDataSources);
}

public interface IDataSourceRouteAssert<T> : IDataSourceRouteAssert where T : class
{

}