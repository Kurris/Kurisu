using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Kurisu.AspNetCore.UnifyResultAndValidation.Attributes;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace Kurisu.AspNetCore.UnifyResultAndValidation;

/// <summary>
/// 异常处理接口，继承此接口并使用<see cref="HandleExceptionAttribute{T}"/>标记方法以实现自定义异常处理。
/// </summary>
public interface IFrameworkExceptionHandlers
{
    /// <summary>
    /// 处理异常
    /// </summary>
    /// <param name="ex"></param>
    Task<bool> HandleAsync(Exception ex);

    /// <summary>
    /// 处理方法集合
    /// </summary>
    /// <returns></returns>
    Dictionary<Type, MethodInfo> GetMethods();
}

/// <summary>
/// base异常处理类
/// </summary>
public abstract class BaseFrameworkExceptionHandlers : IFrameworkExceptionHandlers
{
    private Dictionary<Type, MethodInfo> _methods;

    /// <summary>
    /// 处理异常
    /// </summary>
    /// <param name="ex"></param>
    /// <returns></returns>
    public async Task<bool> HandleAsync(Exception ex)
    {
        var methods = GetMethods();
        var exceptionType = ex.GetType();

        if (methods.All(x => x.Key != exceptionType))
        {
            exceptionType = typeof(Exception);
        }

        if (methods.TryGetValue(exceptionType, out var methodInfo))
        {
            var result = methodInfo.Invoke(this, [ex]);
            if (result is Task task)
            {
                await task;
            }

            return true;
        }

        //await DefaultExceptionHandleAsync(ex);

        return false;
    }

    /// <summary>
    /// 获取处理方法集合
    /// </summary>
    /// <returns></returns>
    public Dictionary<Type, MethodInfo> GetMethods()
    {
        if (_methods != null)
        {
            return _methods;
        }

        var methodInfos = GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance);
        //获取所有标记了HandlerExceptionAttribute<T>特性的MethodInfo
        var infos = methodInfos.Where(m =>
        {
            var attributes = m.GetCustomAttributes<Attribute>();
            return attributes.Any(x =>
            {
                var type = x.GetType();
                return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(HandleExceptionAttribute<>);
            });
        }).ToList();

        var dict = new Dictionary<Type, MethodInfo>(infos.Count);

        //比较HandlerExceptionAttribute<T>中的T和方法的参数类型是否一致
        using (LogContext.PushProperty("Prefix", "异常处理器"))
        {
            foreach (var info in infos)
            {
                var attributes = info.GetCustomAttributes<Attribute>();
                foreach (var attribute in attributes)
                {
                    var attributeType = attribute.GetType();
                    if (attributeType.IsGenericType && attributeType.GetGenericTypeDefinition() == typeof(HandleExceptionAttribute<>))
                    {
                        var genericArgument = attributeType.GetGenericArguments().FirstOrDefault();
                        var parameters = info.GetParameters();
                        if (parameters.Length == 1 && parameters[0].ParameterType == genericArgument)
                        {
                            App.Logger.LogDebug("Method: {InfoName}, Exception Type: {GenericArgumentName}", info.Name, genericArgument.Name);
                            dict.Add(genericArgument, info);
                        }
                    }
                }
            }
        }

        _methods = dict;
        return _methods;
    }
}