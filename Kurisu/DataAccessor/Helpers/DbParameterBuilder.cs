using System;
using System.Collections.Generic;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace Kurisu.DataAccessor.Helpers
{
   
    /// <summary>
    /// 数据库参数建造帮助类
    /// </summary>
    internal class DbParameterBuilder
    {

        internal DbParameterBuilder(DbContext dbContext)
        {
            this._dbContext = dbContext;
        }


        private readonly List<DbParameter> _dbParameters = new List<DbParameter>();
        private readonly DbContext _dbContext;
        private DbProviderFactory _dbProviderFactory = null;

        /// <summary>
        /// 数据库引擎工厂
        /// </summary>
        internal DbProviderFactory DbProviderFactory
        {
            get
            {
                if (_dbProviderFactory == null)
                {
                    if (_dbContext == null)
                    {
                        throw new ArgumentNullException("DbContext 不能为空");
                    }
                    _dbProviderFactory = DbProviderFactories.GetFactory(_dbContext.Database.GetDbConnection());
                }
                return _dbProviderFactory;
            }
        }

        /// <summary>
        /// 获取参数数组
        /// </summary>
        /// <returns></returns>
        internal DbParameter[] GetParams()
        {
            var vals = _dbParameters.ToArray();
            _dbParameters.Clear();
            return vals;
        }

        /// <summary>
        /// 添加参数
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        internal DbParameterBuilder AddParams(IDictionary<string,object> dicParams)
        {
            foreach (var item in dicParams)
            {
                var para = DbProviderFactory.CreateParameter();
                para.ParameterName = item.Key;
                para.Value = item.Value;
                _dbParameters.Add(para);
            }
            return this;
        }
    }
}