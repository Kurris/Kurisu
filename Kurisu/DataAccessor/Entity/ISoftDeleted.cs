using System;

namespace Kurisu.DataAccessor.Entity
{
    /// <summary>
    /// 是否软删除
    /// </summary>
    public interface ISoftDeleted
    {
        /// <summary>
        /// 删除时间,存在即被软删除
        /// </summary>
        public DateTime? DeleteTimed { get; set; }
    }
}