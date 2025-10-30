using Microsoft.Extensions.Configuration;
using System.Net;
using Kurisu.RemoteCall.Utils;
using System.Collections;

namespace Kurisu.RemoteCall.Default;

/// <summary>
/// Url处理器
/// </summary>
internal class DefaultRemoteCallUrlResolver : BaseRemoteCallUrlResolver
{
    private readonly ICommonUtils _commonUtils;

    /// <summary>
    /// init
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="commonUtils"></param>
    public DefaultRemoteCallUrlResolver(IConfiguration configuration, ICommonUtils commonUtils) : base(configuration)
    {
        _commonUtils = commonUtils;
    }

    /// <inheritdoc />
    protected override string ResolveTemplateUrl(string httpMethod, string template, List<ParameterValue> parameters)
    {
        template = base.ResolveTemplateUrl(httpMethod, template, parameters);
        template = ResolveRouteUrl(template, parameters);

        return httpMethod switch
        {
            "GET" => ResolveGetUrl(template, parameters),
            "PUT" or "POST" or "DELETE" or "PATCH" => ResolvePostUrl(template, parameters),
            _ => template
        };
    }

    private string ResolveGetUrl(string template, List<ParameterValue> parameters)
    {
        //处理请求url的地址
        List<string> items = new(parameters.Count);

        //第一个参数为类,并且定义了RequestQueryAttribute
        var parameter = parameters.FirstOrDefault(x => x.QueryAttribute != null
                                                       && TypeHelper.IsComplexType(x.Parameter.ParameterType));
        if (parameter != null)
        {
            // 使用属性名（如果有）作为前缀，方便生成形如 prefix.prop=val 的键名
            var prefix = parameter.QueryAttribute.Name ?? string.Empty;

            // 如果传入的是集合且不是 string，按元素展开为多个同名 query（prefix=1&prefix=2...）
            if (parameter.Value is IEnumerable enumerable and not string)
            {
                prefix = parameter.QueryAttribute.Name ?? parameter.Parameter.Name;
                foreach (var elem in enumerable)
                {
                    if (elem == null) continue;

                    // 简单类型或 string 直接作为值，否则对复杂元素展开成字典再追加
                    if (TypeHelper.IsSimpleType(elem.GetType()))
                    {
                        var k = WebUtility.UrlEncode(prefix);
                        var v = WebUtility.UrlEncode(elem + string.Empty);
                        items.Add($"{k}={v}");
                    }
                    else
                    {
                        var dict = _commonUtils.ToObjDictionary(prefix, elem);
                        foreach (var kv in dict)
                        {
                            var k = WebUtility.UrlEncode(kv.Key);
                            var v = WebUtility.UrlEncode(kv.Value + string.Empty);
                            items.Add($"{k}={v}");
                        }
                    }
                }
            }
            else
            {
                //对象转字典（保留原有行为）
                var objDictionary = _commonUtils.ToObjDictionary(prefix, parameter.Value);
                foreach (var keyValuePair in objDictionary)
                {
                    var k = WebUtility.UrlEncode(keyValuePair.Key);
                    var v = WebUtility.UrlEncode(keyValuePair.Value + string.Empty);
                    items.Add($"{k}={v}");
                }
            }
        }
        else
        {
            //处理简单类型参数
            foreach (var item in parameters.Where(x => TypeHelper.IsSimpleType(x.Parameter.ParameterType)))
            {
                var name = item.QueryAttribute == null ? item.Parameter.Name : item.QueryAttribute.Name;
                var k = WebUtility.UrlEncode(name);
                var v = WebUtility.UrlEncode(item.Value + string.Empty);
                items.Add($"{k}={v}");
            }
        }

        return items.Count > 0
            ? template + "?" + string.Join("&", items)
            : template;
    }


    private static string ResolvePostUrl(string template, List<ParameterValue> parameters)
    {
        foreach (var item in parameters.Where(x => !x.Parameter.ParameterType.IsClass || x.Parameter.ParameterType == typeof(string)))
        {
            if (template.Contains($"{{{item.Parameter.Name}}}"))
            {
                var val = WebUtility.UrlEncode(item.Value + string.Empty);
                template = template.Replace($"{{{item.Parameter.Name}}}", val);
            }
        }

        return template;
    }

    private static string ResolveRouteUrl(string template, List<ParameterValue> parameters)
    {
        foreach (var item in parameters.Where(x => x.RouteAttribute != null))
        {
            var name = $"{{{item.RouteAttribute.Name ?? item.Parameter.Name}}}";
            var value = string.Empty;

            //如果是枚举类型,则获取枚举的名称
            if (item.Parameter.ParameterType.IsEnum)
            {
                value = Enum.GetName(item.Parameter.ParameterType, item.Value) ?? value;
            }
            else
            {
                value = WebUtility.UrlEncode(item.Value + string.Empty);
            }

            template = template.Replace(name, value);
        }

        return template;
    }
}