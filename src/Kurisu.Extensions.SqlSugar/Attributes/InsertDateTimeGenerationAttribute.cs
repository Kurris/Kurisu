using System;

namespace Kurisu.Extensions.SqlSugar.Attributes;

/// <summary>
/// 新增时候自动创建时间
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class InsertDateTimeGenerationAttribute : Attribute
{
}
