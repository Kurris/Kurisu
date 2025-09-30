using Microsoft.Extensions.Logging;

namespace Kurisu.AspNetCore.UnifyResultAndValidation;

/// <summary>
/// 请求配置
/// </summary>
internal class ApiRequestSettingService
{
    private readonly ILogger<App> _logger;

    public ApiRequestSettingService(ILogger<App> logger)
    {
        _logger = logger;
    }

    public bool EnableApiRequestLog { get; set; } = true;
    public string ConnectionId { get; set; }

    public string Path { get; set; }

    public string Method { get; set; }

    /// <summary>
    /// 请求参数
    /// </summary>
    public string Parameters { get; set; }

    public string Response { get; set; }

    public bool LogHandled { get; set; }

    public bool IsGlobal { get; set; }

    public string UserId { get; set; }

    public void Log()
    {
        if (LogHandled)
        {
            return;
        }

        _logger.LogInformation("ConnectionId:{connectionId} UserId:{userId} {method} {path}\r\nRequest:{params}\r\nResponse:{response}", ConnectionId, UserId, Method, Path, Parameters, Response);
        LogHandled = true;
    }
}