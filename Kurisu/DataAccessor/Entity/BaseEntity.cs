using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Kurisu.DataAccessor.Entity
{
    /// <summary>
    /// 基础实体
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    public abstract class BaseEntity<TKey>
    {
        /// <summary>
        /// 主键
        /// </summary>
        [Key]
        public TKey Id { get; set; }

        /// <summary>
        /// 创建人
        /// </summary>
        [Required(ErrorMessage = "创建人不能为空")]
        [Range(0, int.MaxValue)]
        public int CreatedBy { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        [Column(TypeName = "datetime(3)")]
        [Required(ErrorMessage = "创建时间不能为空")]
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 修改人
        /// </summary>
        public int? ModifiedBy{ get; set; }

        /// <summary>
        /// 修改时间
        /// </summary>
        [Column(TypeName = "datetime(3)")]
        public DateTime? ModifiedTime { get; set; }
    }
}