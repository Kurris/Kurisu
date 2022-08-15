using System;

namespace Kurisu.DataAccessor.Entity
{
    /// <summary>
    /// 是否软删除
    /// </summary>
    public interface ISoftDeleted
    {
        /// <summary>
        /// 是否软删除
        /// <remarks>
        /// 如果主动设置为true,那么在删除时,将视为物理删除
        /// </remarks>
        /// </summary>
        bool IsDeleted { get; set; }
    }
}