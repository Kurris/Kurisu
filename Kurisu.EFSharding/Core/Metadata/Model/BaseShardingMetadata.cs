using System.Linq.Expressions;
using System.Reflection;
using Kurisu.EFSharding.Exceptions;
using Kurisu.EFSharding.Extensions.InternalExtensions;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Kurisu.EFSharding.Core.Metadata.Model;

/// <summary>
/// 分片元数据
/// </summary>
public abstract class BaseShardingMetadata
{
    private const string QueryFilter = "QueryFilter";

    public BaseShardingMetadata(Type entityType)
    {
        ClrType = entityType;
    }

    public abstract bool IsDatasourceMetadata { get; }
    public abstract bool IsTableMetadata { get; }

    public Type ClrType { get; }

    public IDictionary<string, PropertyInfo> Properties { get; } = new Dictionary<string, PropertyInfo>();

    public PropertyInfo Property { get; private set; }

    public string TableName { get; private set; }

    public bool IsView { get; private set; }

    public string Schema { get; private set; }


    public List<PropertyInfo?> PrimaryKeys { get; private set; }

    public LambdaExpression QueryFilterExpression { get; private set; }

    /// <summary>
    /// 是否单主键
    /// </summary>
    public bool IsSingleKey => PrimaryKeys.Count == 1;

    public void SetEntityModel(IEntityType entityType)
    {
        if (entityType == null) throw new ArgumentNullException(nameof(entityType));

        TableName = entityType.GetEntityTypeTableName();
        Schema = entityType.GetEntityTypeSchema();
        if (string.IsNullOrWhiteSpace(TableName))
        {
            IsView = entityType.GetEntityTypeIsView();
            if (IsView)
            {
                TableName = entityType.GetEntityTypeViewName();
                Schema = entityType.GetEntityTypeViewSchema();
            }
        }

        QueryFilterExpression = entityType.GetAnnotations().FirstOrDefault(o => o.Name == QueryFilter)?.Value as LambdaExpression;
        PrimaryKeys = entityType.FindPrimaryKey()?.Properties.Select(o => o.PropertyInfo)?.ToList() ?? new List<PropertyInfo>();
    }

    public virtual void SetProperty(PropertyInfo propertyInfo)
    {
        if (Properties.ContainsKey(propertyInfo.Name))
            throw new ShardingCoreConfigException($"same sharding table property name:[{propertyInfo.Name}] don't repeat add");

        Property = propertyInfo;
        Properties.Add(propertyInfo.Name, propertyInfo);
    }


    public virtual void SetExtraProperty(PropertyInfo propertyInfo)
    {
        if (Properties.ContainsKey(propertyInfo.Name))
            throw new ShardingCoreConfigException($"same sharding table property name:[{propertyInfo.Name}] don't repeat add");

        Properties.Add(propertyInfo.Name, propertyInfo);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && ClrType == ((BaseShardingMetadata) obj).ClrType;
    }


    public override int GetHashCode()
    {
        return ClrType.GetHashCode();
    }
}