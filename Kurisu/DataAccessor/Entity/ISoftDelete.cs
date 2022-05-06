using System;

namespace Kurisu.DataAccessor.Entity
{
    public interface ISoftDelete
    {
        public DateTime? DeleteTime { get; set; }
    }
}