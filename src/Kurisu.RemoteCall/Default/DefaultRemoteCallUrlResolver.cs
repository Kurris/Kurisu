using System.Collections;
using Microsoft.Extensions.Configuration;
using System.Net;
using Kurisu.RemoteCall.Utils;

namespace Kurisu.RemoteCall.Default;

/// <summary>
/// Url处理器
/// </summary>
internal class DefaultRemoteCallUrlResolver : BaseRemoteCallUrlResolver
{
    /// <summary>
    /// init
    /// </summary>
    /// <param name="configuration"></param>
    public DefaultRemoteCallUrlResolver(IConfiguration configuration) : base(configuration)
    {
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

    private static string ResolveGetUrl(string template, List<ParameterValue> parameters)
    {
        //处理请求url的地址
        List<string> items = new(parameters.Count);

        //第一个参数为类,并且定义了RequestQueryAttribute
        var parameter = parameters.FirstOrDefault(x => x.QueryAttribute != null && !TypeHelper.IsSimpleType(x.Parameter.ParameterType));
        if (parameter != null)
        {
            //对象转字典
            var objDictionary = parameter.Value.ToObjDictionary(null);
            foreach (var keyValuePair in objDictionary)
            {
                items.Add($"{keyValuePair.Key}={keyValuePair.Value}");
            }
        }
        else
        {
            foreach (var item in parameters)
            {
                var k = WebUtility.UrlEncode(item.Parameter.Name);
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