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
        /// </summary>
        bool IsDeleted { get; set; }
    }
}