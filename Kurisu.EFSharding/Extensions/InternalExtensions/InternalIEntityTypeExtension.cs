using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Kurisu.EFSharding.Extensions.InternalExtensions;

internal static class InternalIEntityTypeExtension
{
    /// <summary>
    /// 获取在db context内的数据库表名称对应叫什么
    /// </summary>
    /// <param name="entityType"></param>
    /// <returns></returns>
    public static string GetEntityTypeTableName(this IEntityType entityType)
    {
        var tableName = entityType.GetTableName();
        return tableName;
    }

    public static string GetEntityTypeSchema(this IEntityType entityType)
    {
        var tableName = entityType.GetSchema();
        return tableName;
    }

    public static bool GetEntityTypeIsView(this IEntityType entityType)
    {
        return !string.IsNullOrWhiteSpace(entityType.GetViewName());
    }

    public static string GetEntityTypeViewName(this IEntityType entityType)
    {
        return entityType.GetViewName();
    }

    public static string GetEntityTypeViewSchema(this IEntityType entityType)
    {
        return entityType.GetViewSchema();
    }
}