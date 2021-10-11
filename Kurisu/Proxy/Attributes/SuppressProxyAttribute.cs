using System;

namespace Kurisu.Proxy.Attributes
{
    /// <summary>
    /// 注销代理方法
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class SuppressProxyAttribute : Attribute
    {
    }
}