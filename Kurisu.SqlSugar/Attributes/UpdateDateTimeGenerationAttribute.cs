namespace Kurisu.SqlSugar.Attributes;

/// <summary>
/// 修改时自动创建时间
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class UpdateDateTimeGenerationAttribute : Attribute
{
}
