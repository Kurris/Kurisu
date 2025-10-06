using AspectCore.DynamicProxy.Parameters;
using Kurisu.AspNetCore.Abstractions.UnifyResultAndValidation;
using Kurisu.Test.WebApi_A.Controllers;

namespace Kurisu.Test.WebApi_A.Aops;

[AttributeUsage(AttributeTargets.All)]
public class CheckMyLessonAOP : ParameterInterceptorAttribute
{
    public override async Task Invoke(ParameterAspectContext context, ParameterAspectDelegate next)
    {
        if (context.Parameter.Value is ILessonId lesson)
        {
            //lesson.LessonId;
        }

        await next(context);
    }
}

[AttributeUsage(AttributeTargets.Method)]
public class HandleException111Attribute<T> : Attribute where T : Exception
{
    /// <summary>
    /// 异常处理类
    /// </summary>
    public Type Type => typeof(T);
}

public class TestReturnParameterAOP : ReturnParameterInterceptorAttribute
{
    public override async Task Invoke(ParameterAspectContext context, ParameterAspectDelegate next)
    {
        var a = context.Parameter.Value as Task<IApiResult>;
        await next(context);
    }
}