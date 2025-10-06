using Kurisu.AspNetCore.Abstractions.DataAccess.Contract;
using Kurisu.AspNetCore.Abstractions.UnifyResultAndValidation;
using SqlSugar;

namespace Kurisu.Extensions.SqlSugar.Extensions;

/// <summary>
/// 执行扩展
/// </summary>
public static class ExecuteCommandExtensions
{
    /// <summary>
    /// 没有影响行则抛出Version异常
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="updateable"></param>
    /// <param name="errorMessage"></param>
    /// <returns></returns>
    /// <exception cref="VersionExceptions"></exception>
    public static async Task ExecuteCommandThrowVersionAsync<T>(this IUpdateable<T> updateable, string errorMessage = "执行失败,稍后请重试...")
        where T : class, IVersionId, new()
    {
        var effect = await updateable.ExecuteCommandAsync();
        if (effect <= 0)
        {
            throw new VersionExceptions(errorMessage);
        }
    }


    /// <summary>
    /// 没有影响行则抛出Version异常
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="updateable"></param>
    /// <param name="errorMessage"></param>
    /// <exception cref="VersionExceptions"></exception>
    public static void ExecuteCommandThrowVersion<T>(this IUpdateable<T> updateable, string errorMessage = "执行失败,稍后请重试...")
      where T : class, IVersionId, new()
    {
        var effect = updateable.ExecuteCommand();
        if (effect <= 0)
        {
            throw new VersionExceptions(errorMessage);
        }
    }


    /// <summary>
    /// 重试执行
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="updateable"></param>
    /// <param name="errorMessage"></param>
    /// <param name="retry"></param>
    /// <returns></returns>
    /// <exception cref="VersionExceptions"></exception>
    public static async Task ExecuteCommandRetryAsync<T>(this IUpdateable<T> updateable, string errorMessage = "执行失败,稍后请重试...", int retry = 3)
        where T : class, IVersionId, new()
    {
        await RetryAsync(async () =>
        {
            var effect = await updateable.ExecuteCommandAsync();
            if (effect <= 0)
            {
                throw new VersionExceptions(errorMessage);
            }
        }, retry: retry, errorMessage: errorMessage);
    }

    /// <summary>
    /// 重试执行
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="updateable"></param>
    /// <param name="errorMessage"></param>
    /// <param name="retry"></param>
    /// <exception cref="VersionExceptions"></exception>
    public static void ExecuteCommandRetry<T>(this IUpdateable<T> updateable, string errorMessage = "执行失败,稍后请重试...", int retry = 3)
      where T : class, IVersionId, new()
    {
        Retry(() =>
        {
            var effect = updateable.ExecuteCommand();
            if (effect <= 0)
            {
                throw new VersionExceptions(errorMessage);
            }
        }, retry: retry, errorMessage: errorMessage);
    }



    private static async Task RetryAsync(Func<Task> func, int retry = 3, string errorMessage = null)
    {
        for (int i = 0; i < retry; i++)
        {
            try
            {
                await func();
                return;
            }
            catch (Exception ex) when (ex is VersionExceptions)
            {
                await Task.Delay(50);
            }
        }

        throw new UserFriendlyException(errorMessage ?? "操作失败,请重试");
    }


    private static void Retry(Action action, int retry = 3, string errorMessage = null)
    {
        for (int i = 0; i < retry; i++)
        {
            try
            {
                action.Invoke();
                return;
            }
            catch (Exception ex) when (ex is VersionExceptions)
            {
                Task.Delay(50).Wait();
            }
        }

        throw new UserFriendlyException(errorMessage ?? "操作失败,请重试");
    }
}
