using System;

namespace Kurisu
{
    /// <summary>
    /// App内部
    /// </summary>
    internal class InternalApp
    {
        /// <summary>
        /// 根服务提供器
        /// </summary>
        internal static IServiceProvider ApplicationServices { get; set; }
    }
}