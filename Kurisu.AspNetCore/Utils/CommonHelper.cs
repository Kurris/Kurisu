using System;
using System.Threading.Tasks;
using Kurisu.AspNetCore.UnifyResultAndValidation;
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
}
