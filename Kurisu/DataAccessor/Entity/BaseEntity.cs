using System;
using System.ComponentModel.DataAnnotations;

namespace Kurisu.DataAccessor.Entity
{
    /// <summary>
    /// 基础实体
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    public class BaseEntity<TKey> : IBaseEntity
    {
        /// <summary>
        /// 主键
        /// </summary>
        [Key]
        public TKey Id { get; set; }

        /// <summary>
        /// 创建人
        /// </summary>
        [Required]
        [Range(0, int.MaxValue)]
        public int Creator { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Required(ErrorMessage = "创建时间不能为空")]
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 更新人
        /// </summary>
        public int? Updater { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime? UpdateTime { get; set; }
    }
}