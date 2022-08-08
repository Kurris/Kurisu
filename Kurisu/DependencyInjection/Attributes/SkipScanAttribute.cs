using System;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// 跳过自动扫描
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
    public class SkipScanAttribute : Attribute
    {
    }
}