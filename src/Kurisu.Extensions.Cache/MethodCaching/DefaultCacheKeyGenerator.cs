using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using AspectCore.DynamicProxy;
using Kurisu.AspNetCore.Abstractions.Cache;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Kurisu.Extensions.Cache.MethodCaching;

public sealed class DefaultCacheKeyGenerator(IOptions<MethodCacheOptions> options) : ICacheKeyGenerator
{
    public string Generate(AspectContext context, string region, MethodCachePolicy policy, MethodCacheScope scope)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(region);
        var method = context.ServiceMethod;
        var parameters = method.GetParameters();

        if (parameters.Any(p => p.ParameterType.IsByRef)) throw new NotSupportedException("方法缓存不支持 ref/out 参数。");

        var indices = Enumerable.Range(0, parameters.Length)
            .Where(i => parameters[i].ParameterType != typeof(CancellationToken));
        return GenerateCore(region, $"{method.DeclaringType?.FullName}:{method}",
            indices.Select(i => new { Type = parameters[i].ParameterType.FullName, Value = context.Parameters[i] }).ToArray(), policy, scope);
    }

    public string Generate(string region, object value, MethodCachePolicy policy, MethodCacheScope scope)
        => GenerateCore(region, null, new[] { new { Type = value?.GetType().FullName, Value = value } }, policy, scope);

    private string GenerateCore(string region, string method, object arguments, MethodCachePolicy policy, MethodCacheScope scope)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(region);
        var key = new
        {
            options.Value.KeyPrefix,
            Region = region,
            policy.Version,
            Scope = scope.Dimensions,
            Method = method,
            Arguments = arguments
        };

        var serializer = JsonSerializer.Create(new JsonSerializerSettings
        {
            Culture = CultureInfo.InvariantCulture,
            TypeNameHandling = TypeNameHandling.None,
            ReferenceLoopHandling = ReferenceLoopHandling.Error
        });
        var json = Canonicalize(JToken.FromObject(key, serializer)).ToString(Formatting.None);
        return $"{options.Value.KeyPrefix}:data:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)))}";
    }

    private static JToken Canonicalize(JToken token) => token switch
    {
        JObject obj => new JObject(obj.Properties().OrderBy(p => p.Name, StringComparer.Ordinal)
            .Select(p => new JProperty(p.Name, Canonicalize(p.Value)))),
        JArray array => new JArray(array.Select(Canonicalize)),
        _ => token.DeepClone()
    };
}