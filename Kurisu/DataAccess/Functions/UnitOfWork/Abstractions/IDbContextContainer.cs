//using System;
//using System.Threading.Tasks;
//using Microsoft.EntityFrameworkCore;

//namespace Kurisu.DataAccess.Functions.UnitOfWork.Abstractions;

///// <summary>
///// 工作单元数据库上下文容器
///// </summary>
//public interface IDbContextContainer : IDisposable
//{
//    /// <summary>
//    /// 数据库上下文个数
//    /// </summary>
//    public int Count { get; }

//    /// <summary>
//    /// 是否自动提交
//    /// </summary>
//    public bool IsAutomaticSaveChanges { get; set; }

//    /// <summary>
//    /// 是否运行中
//    /// <remarks>
//    /// 当前方法是否在UnitOfWork中执行
//    /// </remarks>
//    /// </summary>
//    public bool IsRunning { get; }

//    /// <summary>
//    /// 添加上下文到容器中
//    /// </summary>
//    /// <param name="dbContext"></param>
//    /// <returns></returns>
//    void Manage(DbContext dbContext);

//    /// <summary>
//    /// 保存所有数据库上下文的更改
//    /// </summary>
//    /// <returns></returns>
//    Task<int> SaveChangesAsync();

//    /// <summary>
//    /// 保存所有数据库上下文的更改
//    /// </summary>
//    /// <param name="acceptAllChangesOnSuccess"></param>
//    /// <returns></returns>
//    int SaveChanges(bool acceptAllChangesOnSuccess);


//    /// <summary>
//    /// 保存所有数据库上下文的更改
//    /// </summary>
//    /// <returns></returns>
//    int SaveChanges();

//    /// <summary>
//    /// 保存所有数据库上下文的更改
//    /// </summary>
//    /// <param name="acceptAllChangesOnSuccess"></param>
//    /// <returns></returns>
//    Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess);

//    /// <summary>
//    /// 打开事务
//    /// </summary>
//    /// <returns></returns>
//    Task<IDbContextContainer> BeginTransactionAsync();


//    /// <summary>
//    /// 打开事务
//    /// </summary>
//    /// <returns></returns>
//    IDbContextContainer BeginTransaction();

//    /// <summary>
//    /// 提交事务
//    /// </summary>
//    /// <param name="acceptAllChangesOnSuccess"></param>
//    /// <param name="exception"></param>
//    Task CommitTransactionAsync(bool acceptAllChangesOnSuccess, Exception exception = null);

//    /// <summary>
//    /// 提交事务
//    /// </summary>
//    /// <param name="acceptAllChangesOnSuccess"></param>
//    /// <param name="exception"></param>
//    void CommitTransaction(bool acceptAllChangesOnSuccess, Exception exception = null);
//}