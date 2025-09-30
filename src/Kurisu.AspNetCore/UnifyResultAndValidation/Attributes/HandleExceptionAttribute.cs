using System;

namespace Kurisu.AspNetCore.UnifyResultAndValidation.Attributes;

/// <summary>
/// 标记处理的异常类型
/// </summary>
/// <typeparam name="T"></typeparam>
[AttributeUsage(AttributeTargets.Method)]
public class HandleExceptionAttribute<T> : Attribute where T : Exception
{
    /// <summary>
    /// 异常处理类
    /// </summary>
    public Type Type => typeof(T);
}