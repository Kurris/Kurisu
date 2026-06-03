using System;

namespace Kurisu.AspNetCore.UnifyResultAndValidation.Attributes;

/// <summary>
/// 跳过统一成功结果包装。
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true)]
public class SkipPackResultAttribute : Attribute
{
}
