using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Kurisu.EFSharding.Sharding.PaginationConfigurations;


public class PaginationSequenceConfig
{
    public PaginationSequenceConfig(LambdaExpression orderPropertyExpression, PaginationMatchEnum paginationMatchEnum= PaginationMatchEnum.Owner, IComparer<string> routeComparer=null)
    {
        OrderPropertyInfo = orderPropertyExpression.GetPropertyAccess();
        PropertyName = OrderPropertyInfo.Name;
        PaginationMatchEnum = paginationMatchEnum;
        RouteComparer = routeComparer ?? Comparer<string>.Default;
        SequenceTails = new HashSet<string>();
    }

    public IComparer<string> RouteComparer { get; set; }
    public PaginationMatchEnum PaginationMatchEnum { get; set; }
    public PropertyInfo OrderPropertyInfo { get; set; }

    /// <summary>
    /// 如果查询没发现排序就将当前配置追加上去
    /// </summary>
    public bool AppendIfOrderNone => AppendOrder >= 0;
    /// <summary>
    /// 大于等于0表示需要
    /// </summary>
    public int AppendOrder { get; set; } = -1;

    public bool AppendAsc { get; set; } = true;
    public string PropertyName { get;}

    public ISet<string> SequenceTails { get; }


    protected bool Equals(PaginationSequenceConfig other)
    {
        return PropertyName == other.PropertyName;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((PaginationSequenceConfig) obj);
    }

    public override int GetHashCode()
    {
        return (PropertyName != null ? PropertyName.GetHashCode() : 0);
    }
}