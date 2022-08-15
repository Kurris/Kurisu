using System;
using Kurisu.Scope.Abstractions;
using Kurisu.Scope.Internal;

namespace Kurisu.Scope
{
    /// <summary>
    /// 局部作用域
    /// </summary>
    public sealed class Scoped
    {
        /// <summary>
        /// 临时作用域
        /// </summary>
        public static Lazy<IScope> Template { get; set; } = new(() => new TemplateScope());

        /// <summary>
        /// 请求作用域
        /// </summary>
        public static Lazy<IScope> Request { get; set; } = new(() => new RequestScope());
    }
}