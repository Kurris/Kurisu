using System;

namespace Kurisu.AspNetCore.DataAccess.SqlSugar.Attributes;

/// <summary>
/// 标记为Log日志的key
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class LogKeyAttribute : Attribute
{
}
