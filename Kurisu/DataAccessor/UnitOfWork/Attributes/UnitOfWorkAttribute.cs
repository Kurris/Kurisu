using System;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Kurisu.DataAccessor.UnitOfWork.Attributes
{
    /// <summary>
    /// 工作单元
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class UnitOfWorkAttribute : Attribute
    {
        /// <summary>
        /// 是否为手动保存更改
        /// </summary>
        private readonly bool _isManualSaveChanges;

        public UnitOfWorkAttribute() : this(false)
        {
        }

        public UnitOfWorkAttribute(bool isManualSaveChanges)
        {
            _isManualSaveChanges = isManualSaveChanges;
        }

        /// <summary>
        /// 是否为手动保存更改
        /// </summary>
        public bool IsManualSaveChanges => _isManualSaveChanges;
    }
}