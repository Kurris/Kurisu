using Kurisu.AspNetCore.ConfigurableOptions.Attributes;

namespace Kurisu.AspNetCore.UnifyResultAndValidation.Options;

/// <summary>
/// 过滤器选项
/// </summary>
[Configuration]
public class FilterOptions
{
    /// <summary>
    /// 启用api请求日志
    /// </summary>
    public bool EnableApiRequestLog { get; set; }
}
