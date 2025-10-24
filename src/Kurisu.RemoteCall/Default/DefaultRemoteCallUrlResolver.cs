using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

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
            "PUT" => ResolvePutUrl(template, parameters),
            "POST" => ResolvePostUrl(template, parameters),
            "DELETE" => ResolveDeleteUrl(template, parameters),
            "PATCH" => ResolvePutUrl(template, parameters),
            _ => template
        };
    }


    private static string ResolveGetUrl(string template, List<ParameterValue> parameters)
    {
        //处理请求url的地址
        List<string> items = new(parameters.Count);

        //第一个参数为类,并且定义了RequestQueryAttribute
        var parameter = parameters.FirstOrDefault(x => x.QueryAttribute != null && x.Parameter.ParameterType != typeof(string));
        if (parameter != null)
        {
            //对象转字典
            var d = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(parameter.Value))!;
            items.AddRange(d.Select(pair => $"{pair.Key}={pair.Value}"));
        }

        else
        {
            foreach (var item in parameters)
            {
                if (template.Contains($"{{{item.Parameter.Name}}}"))
                {
                    template = template.Replace($"{{{item.Parameter.Name}}}", item.Value + string.Empty);
                }
                else
                {
                    items.Add($"{item.Parameter.Name}={item.Value}");
                }
            }
        }

        return items.Count > 0
            ? template + "?" + string.Join("&", items)
            : template;
    }

    private static string ResolveDeleteUrl(string template, List<ParameterValue> parameters)
    {
        foreach (var item in parameters)
        {
            if (template.Contains($"{{{item.Parameter.Name}}}"))
            {
                template = template.Replace($"{{{item.Parameter.Name}}}", item.Value + string.Empty);
            }
        }

        return template;
    }

    private static string ResolvePutUrl(string template, List<ParameterValue> parameters)
    {
        foreach (var item in parameters.Where(x => !x.Parameter.ParameterType.IsClass || x.Parameter.ParameterType == typeof(string)))
        {
            if (template.Contains($"{{{item.Parameter.Name}}}"))
            {
                template = template.Replace($"{{{item.Parameter.Name}}}", item.Value + string.Empty);
            }
        }

        return template;
    }

    private static string ResolvePostUrl(string template, List<ParameterValue> parameters)
    {
        foreach (var item in parameters.Where(x => !x.Parameter.ParameterType.IsClass || x.Parameter.ParameterType == typeof(string)))
        {
            if (template.Contains($"{{{item.Parameter.Name}}}"))
            {
                template = template.Replace($"{{{item.Parameter.Name}}}", item.Value + string.Empty);
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
                value = item.Value + string.Empty;
            }

            template = template.Replace(name, value);
        }

        return template;
    }
}