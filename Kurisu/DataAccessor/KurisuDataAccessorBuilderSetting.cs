namespace Kurisu.DataAccessor
{
    /// <summary>
    /// 数据访问builder 配置
    /// </summary>
    public class KurisuDataAccessorBuilderSetting
    {
        /// <summary>
        /// 是否开启软删除(默认:true)
        /// </summary>
        public bool IsEnableSoftDeleted { get; set; } = true;

        /// <summary>
        /// 是否开启读写分离
        /// </summary>
        public bool IsEnableReadWriteSplit { get; set; }

        /// <summary>
        /// 是否开启工作单元
        /// </summary>
        public bool IsEnableUnitOfWork { get; set; }

        /// <summary>
        /// 是否开启多租户
        /// </summary>
        public bool IsEnableMultiTenant { get; set; }
    }
}