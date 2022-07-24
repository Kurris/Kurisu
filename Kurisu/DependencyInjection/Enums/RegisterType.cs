// ReSharper disable once CheckNamespace

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// 依赖注入类型
    /// </summary>
    [SkipScan]
    public enum RegisterType
    {
        Transient,
        Scoped,
        Singleton
    }
}