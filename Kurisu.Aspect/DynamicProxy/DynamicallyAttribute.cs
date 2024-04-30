namespace Kurisu.Aspect.DynamicProxy;

/// <summary>
/// 标记为代理类
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
internal sealed class DynamicallyAttribute : Attribute
{
}