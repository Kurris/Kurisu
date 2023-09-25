using System.Linq.Expressions;

namespace Kurisu.EFSharding.Sharding.MergeContexts;


public class GroupByContext
{
    /// <summary>
    /// group by 表达式
    /// </summary>
    public LambdaExpression GroupExpression { get; set; }
    /// <summary>
    /// 是否内存聚合
    /// </summary>
    public bool GroupMemoryMerge { get; set; }
    public List<PropertyOrder> PropertyOrders { get; } = new List<PropertyOrder>();
    public string GetOrderExpression()
    {
        return string.Join(",", PropertyOrders);
    }

}