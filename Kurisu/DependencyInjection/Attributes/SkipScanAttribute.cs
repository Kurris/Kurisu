using System;

namespace Kurisu.DependencyInjection.Attributes
{
    /// <summary>
    /// 跳过自动扫描
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class SkipScanAttribute : Attribute
    {
    }
}