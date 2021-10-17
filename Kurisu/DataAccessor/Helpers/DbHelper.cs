using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Kurisu.DataAccessor.Helpers
{
    internal class DbHelper
    {
        private readonly DbContext _dbContext = null;
        private readonly DbParameterBuilder _dbParameterBuilder = null;

        /// <summary>
        /// ADO帮助类
        /// </summary>
        /// <param name="dbContext">数据库上下文</param>
        internal DbHelper(DbContext dbContext)
        {
            _dbContext = dbContext;
            _dbParameterBuilder = new DbParameterBuilder(_dbContext);
        }

        /// <summary>
        /// 创建DbCommand对象,打开连接,打开事务
        /// </summary>
        /// <param name="sql">执行语句</param>
        /// <param name="dicParams">参数</param>
        /// <returns>IDbCommand</returns>
        private async Task<DbCommand> CreateDbCommand(string sql, IDictionary<string, object> dicParams)
        {
            try
            {
                var dbConnection = _dbContext.Database.GetDbConnection();
                var cmd = dbConnection.CreateCommand();

                cmd.CommandText = sql;
                cmd.Transaction = _dbContext.Database.CurrentTransaction?.GetDbTransaction();

                if (dicParams != null && dicParams.Count != 0)
                {
                    cmd.Parameters.AddRange(_dbParameterBuilder.AddParams(dicParams).GetParams());
                }

                if (cmd.Connection.State == ConnectionState.Closed)
                {
                    await cmd.Connection.OpenAsync();
                }

                return cmd;
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// 执行sql
        /// </summary>
        /// <param name="sql">sql</param>
        /// <returns>返回受影响行数<see cref="int"/></returns>
        internal async Task<int> RunSql(string sql) => await RunSql(sql, null);

        /// <summary>
        /// 执行查询语句
        /// </summary>
        /// <param name="sql">执行语句</param>
        /// <param name="dicParams">参数</param>
        /// <returns>受影响的行数<see cref="int"/></returns>
        internal async Task<int> RunSql(string sql, IDictionary<string, object> dicParams)
        {
            using (var cmd = await CreateDbCommand(sql, dicParams))
            {
                try
                {
                    return await cmd.ExecuteNonQueryAsync();
                }
                catch
                {
                    await cmd.DisposeAsync();
                    throw;
                }
            }
        }

        /// <summary>
        /// 执行查询语句,单行
        /// </summary>
        /// <param name="sql">执行语句</param>
        /// <returns>DbDataReader对象</returns>
        internal async Task<IDataReader> GeDataReader(string sql) => await GeDataReader(sql, null);

        /// <summary>
        /// 执行查询语句,单行
        /// </summary>
        /// <param name="sql">执行语句</param>
        /// <param name="commandType">执行命令类型</param>
        /// <param name="parameters">参数数组</param>
        /// <returns>DbDataReader对象</returns>
        internal async Task<IDataReader> GeDataReader(string sql, IDictionary<string, object> dicParams)
        {
            using (var cmd = await CreateDbCommand(sql, dicParams))
            {
                try
                {
                    return await cmd.ExecuteReaderAsync(CommandBehavior.Default);
                }
                catch
                {
                    await cmd.DisposeAsync();
                    throw;
                }
            }
        }

        /// <summary>
        /// 执行查询语句,返回一个包含查询结果的DataTable
        /// </summary>
        /// <param name="sql">执行语句</param>
        /// <returns>DataTable</returns>
        internal async Task<DataTable> GetDataTable(string sql) => await GetDataTable(sql, null);

        /// <summary>
        /// 执行查询语句,返回一个包含查询结果的DataTable
        /// </summary>
        /// <param name="sql">执行语句</param>
        /// <param name="dicParams">参数</param>
        /// <returns><see cref="DataTable"</returns>
        internal async Task<DataTable> GetDataTable(string sql, IDictionary<string, object> dicParams)
        {
            using (var cmd = await CreateDbCommand(sql, dicParams))
            {
                try
                {
                    using (var adapter = DbProviderFactories.GetFactory(cmd.Connection).CreateDataAdapter())
                    {
                        adapter.SelectCommand = cmd;

                        DataTable dt = new DataTable();
                        adapter.Fill(dt);

                        return dt;
                    }
                }
                catch (Exception ex)
                {
                    await cmd.DisposeAsync();
                    throw ex;
                }
            }
        }


        /// <summary>
        /// 执行一个查询语句，返回查询结果的首行首列
        /// </summary>
        /// <param name="sql">执行语句</param>
        /// <returns>首行首列</returns>
        internal async Task<object> GetScalar(string sql) => await GetScalar(sql, null);

        /// <summary>
        /// 执行一个查询语句，返回查询结果的首行首列
        /// </summary>
        /// <param name="sql">执行语句</param>
        /// <param name="dicParams">参数数</param>
        /// <returns>首行首列object</returns>
        internal async Task<object> GetScalar(string sql, IDictionary<string, object> dicParams)
        {
            using (DbCommand cmd = await CreateDbCommand(sql, dicParams))
            {
                try
                {
                    return await cmd.ExecuteScalarAsync();
                }
                catch
                {
                    await cmd.DisposeAsync();
                    throw;
                }
            }
        }
    }
}