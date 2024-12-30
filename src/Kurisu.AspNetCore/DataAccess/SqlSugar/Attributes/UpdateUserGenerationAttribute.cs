using System;

namespace Kurisu.AspNetCore.DataAccess.SqlSugar.Attributes;

/// <summary>
/// 修改时自动获取当前用户
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class UpdateUserGenerationAttribute : Attribute
{
}
