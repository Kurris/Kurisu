using System.Reflection;

namespace Kurisu.EFSharding.Sharding.Visitors.Selects;

public class SelectAverageProperty : SelectAggregateProperty
{
    /// <summary>
    /// 平均值是通过哪个属性获取的
    /// </summary>
    public PropertyInfo FromProperty { get; }

    public SelectAverageProperty(Type ownerType, PropertyInfo property,PropertyInfo fromProperty, bool isAggregateMethod, string aggregateMethod) : base(ownerType, property, isAggregateMethod, aggregateMethod)
    {
        FromProperty = fromProperty;
    }
    /// <summary>
    /// 求数量的属性
    /// </summary>
    public PropertyInfo CountProperty { get; private set; }
    /// <summary>
    /// 当前属性的求和属性
    /// </summary>
    public PropertyInfo SumProperty { get; private set; }

    public void BindCountProperty(PropertyInfo countProperty)
    {
        CountProperty = countProperty;
    }
    public void BindSumProperty(PropertyInfo sumProperty)
    {
        SumProperty = sumProperty;
    }
}