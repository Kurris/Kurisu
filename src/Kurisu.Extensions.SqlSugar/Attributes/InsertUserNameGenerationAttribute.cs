namespace Kurisu.Extensions.SqlSugar.Attributes;

/// <summary>
/// 新增时自动获取当前用户
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class InsertUserNameGenerationAttribute : Attribute;
