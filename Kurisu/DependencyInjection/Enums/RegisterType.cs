using Kurisu.DependencyInjection.Attributes;

namespace Kurisu.DependencyInjection.Enums
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