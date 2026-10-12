using System.Globalization;
using System.Text;
using AspectCore.DynamicProxy;
using Kurisu.AspNetCore.Abstractions.Cache;
using Microsoft.Extensions.Options;

namespace Kurisu.Extensions.Cache.MethodCaching;

public sealed class DefaultCacheKeyGenerator(IOptions<MethodCacheOptions> options) : ICacheKeyGenerator
{
    public string Generate(AspectContext context, string region, MethodCachePolicy policy, MethodCacheScope scope)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(region);
        var method = context.ServiceMethod;
        var parameters = method.GetParameters();

        if (parameters.Any(p => p.ParameterType.IsByRef)) throw new NotSupportedException("方法缓存不支持 ref/out 参数。");

        if (context.Parameters.Length == 0)
            throw new ArgumentException("未指定缓存 Key 时必须提供首参数；无参数方法请显式指定 Key 表达式。");
        return Generate(region, context.Parameters[0], policy, scope);
    }

    private static string ToKeyString(object value, string description) => value switch
    {
        string text => text,
        int or long => Convert.ToString(value, CultureInfo.InvariantCulture),
        _ => throw new ArgumentException($"{description} 必须为 string、int 或 long，当前类型为 {value?.GetType().FullName ?? "null"}。请显式选择或转换业务 Key。")
    };

    public string Generate(string region, object value, MethodCachePolicy policy, MethodCacheScope scope)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(region);
        var businessKey = ToKeyString(value, "缓存 Key");
        var key = new StringBuilder(options.Value.KeyPrefix).Append(":data:s:").Append(Uri.EscapeDataString(region))
            .Append(':').Append(Uri.EscapeDataString(policy.Version)).Append(":scope:")
            .Append(scope.Dimensions.Count.ToString(CultureInfo.InvariantCulture));
        foreach (var dimension in scope.Dimensions)
        {
            key.Append(':').Append(Uri.EscapeDataString(dimension.Key));
            // 没有等号表示 null，有等号且没有值表示空字符串。
            if (dimension.Value != null)
                key.Append('=').Append(Uri.EscapeDataString(ToKeyString(dimension.Value, $"缓存上下文 '{dimension.Key}'")));
        }

        key.Append(":key:").Append(Uri.EscapeDataString(businessKey));
        return key.ToString();
    }
}