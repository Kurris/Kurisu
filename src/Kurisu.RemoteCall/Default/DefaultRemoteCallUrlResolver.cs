using System.Collections;
using System.Reflection;
using Kurisu.RemoteCall.Attributes;
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
    protected override string ResolveTemplateUrl(HttpMethodType httpMethod, string template, List<ParameterValue> parameters)
    {
        template = base.ResolveTemplateUrl(httpMethod, template, parameters);

        return httpMethod switch
        {
            HttpMethodType.Get => ResolveGetUrl(template, parameters),
            HttpMethodType.Put => ResolvePutUrl(template, parameters),
            HttpMethodType.Post => ResolvePostUrl(template, parameters),
            HttpMethodType.Delete => ResolveDeleteUrl(template, parameters),
            HttpMethodType.Patch => ResolvePutUrl(template, parameters),
            _ => template
        };
    }


    private static string ResolveGetUrl(string template, List<ParameterValue> parameters)
    {
        parameters = parameters.Where(x => !x.Parameter.IsDefined(typeof(RequestIgnoreQueryAttribute))).ToList();
        //处理请求url的地址
        List<string> items = new(parameters.Count);

        var parameter = parameters.FirstOrDefault(x => x.Parameter.IsDefined(typeof(RequestQueryAttribute)) && x.Parameter.ParameterType != typeof(string));
        if (parameter != null)
        {
            var v = parameter.Value;
            if (v is IEnumerable nv)
            {
                foreach (var item in nv)
                {
                    items.Add($"{parameter.Parameter.Name}={item}");
                }
            }
            else
            {
                //对象转字典
                var d = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(parameter.Value))!;
                items.AddRange(d.Select(pair => $"{pair.Key}={pair.Value}"));
            }
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
}