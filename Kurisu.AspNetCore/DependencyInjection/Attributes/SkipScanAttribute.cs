// ReSharper disable once CheckNamespace

using System;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 跳过自动扫描
/// </summary>
[AttributeUsage(AttributeTargets.All, Inherited = false)]
public class SkipScanAttribute : Attribute
{
}