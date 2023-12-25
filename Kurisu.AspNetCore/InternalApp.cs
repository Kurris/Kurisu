using System;

namespace Kurisu.AspNetCore;

/// <summary>
/// App内部
/// </summary>
internal class InternalApp
{
    /// <summary>
    /// 根服务提供器,对应dotnet core在ConfigureServices中配置完成后生成的IServiceProvider
    /// </summary>
    internal static IServiceProvider RootServices { get; set; }
}