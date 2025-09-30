using System;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Kurisu.AspNetCore.UnifyResultAndValidation.Exceptions;

/// <summary>
/// 模型验证异常。
/// </summary>
public class ModelStateNotValidException : Exception
{
    /// <summary>
    /// 初始化 <see cref="ModelStateNotValidException"/> 类的新实例。
    /// </summary>
    /// <param name="context">当前的结果执行上下文。</param>
    public ModelStateNotValidException(ResultExecutingContext context)
    {
        Context = context;
    }

    /// <summary>
    /// 获取结果执行上下文。
    /// </summary>
    public ResultExecutingContext Context { get; }
}