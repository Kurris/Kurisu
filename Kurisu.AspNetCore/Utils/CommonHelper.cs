using System;
using System.Threading.Tasks;
using Kurisu.Core.CustomClass;
using SqlSugar;

namespace Kurisu.AspNetCore.Utils;

/// <summary>
/// 通用帮助
/// </summary>
public class CommonHelper
{
    /// <summary>
    /// 重试
    /// </summary>
    /// <param name="func"></param>
    /// <param name="retry"></param>
    /// <param name="errorMessage"></param>
    /// <returns></returns>
    /// <exception cref="UserFriendlyException"></exception>
    public static async Task RetryAsync(Func<Task> func, int retry = 3, string errorMessage = null)
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
            catch (Exception)
            {
                throw;
            }
        }

        throw new UserFriendlyException(errorMessage ?? "操作失败,请重试");
    }

    /// <summary>
    /// 重试
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="func"></param>
    /// <param name="retry"></param>
    /// <param name="errorMessage"></param>
    /// <returns></returns>
    /// <exception cref="UserFriendlyException"></exception>
    public static async Task<T> RetryAsync<T>(Func<Task<T>> func, int retry = 3, string errorMessage = null)
    {
        for (int i = 0; i < retry; i++)
        {
            try
            {
                return await func();
            }
            catch (Exception ex) when (ex is VersionExceptions)
            {
                await Task.Delay(50);
            }
            catch (Exception)
            {
                throw;
            }
        }

        throw new UserFriendlyException(errorMessage ?? "操作失败,请重试");
    }
}
