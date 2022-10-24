using System;

namespace Kurisu
{
    /// <summary>
    /// App内部
    /// </summary>
    internal class InternalApp
    {
        /// <summary>
        /// 根服务提供器,对应dotnetcore在ConfigureServices中生成的IServiceProvider
        /// </summary>
        internal static IServiceProvider ApplicationServices { get; set; }
    }
}