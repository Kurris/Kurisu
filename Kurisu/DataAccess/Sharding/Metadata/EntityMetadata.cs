using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Kurisu.DataAccess.Sharding.Metadata;

/// <summary>
/// 实体sharding元数据
/// </summary>
public class EntityMetadata
{
    private const string QueryFilter = "QueryFilter";


    public EntityMetadata(Type type)
    {
        EntityType = type;
    }

    public Type EntityType { get; set; }

    public bool IsSingleKey { get; private set; }

    public bool IsView { get; private set; }

    public string Schema { get; private set; }

    public LambdaExpression QueryFilterExpression { get; private set; }

    /// <summary>
    /// 逻辑表名
    /// </summary>
    public string LogicTableName { get; private set; }


    public PropertyInfo Property { get; private set; }

    /// <summary>
    /// 主键
    /// </summary>
    public IReadOnlyList<PropertyInfo> PrimaryKeyProperties { get; private set; }

    public void SetEntityModel(IEntityType entityType)
    {
        LogicTableName = entityType.GetTableName();
        Schema = entityType.GetSchema();
        if (string.IsNullOrWhiteSpace(LogicTableName))
        {
            IsView = !string.IsNullOrEmpty(entityType.GetViewName());
            if (IsView)
            {
                LogicTableName = entityType.GetViewName();
                Schema = entityType.GetSchema();
            }
        }

        QueryFilterExpression =
            entityType.GetAnnotations().FirstOrDefault(o => o.Name == QueryFilter)?.Value as LambdaExpression;
        PrimaryKeyProperties = entityType.FindPrimaryKey()?.Properties?.Select(o => o.PropertyInfo)?.ToList() ??
                               new List<PropertyInfo>();
        IsSingleKey = PrimaryKeyProperties.Count == 1;
    }

    internal void SetProperty(PropertyInfo propertyInfo)
    {
        Property = propertyInfo;
    }
}