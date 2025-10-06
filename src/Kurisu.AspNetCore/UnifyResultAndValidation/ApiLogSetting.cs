using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;

namespace Kurisu.AspNetCore.UnifyResultAndValidation;

/// <summary>
/// 请求日志设置
/// </summary>
internal class ApiLogSetting
{
    private readonly ILogger<App> _logger;

    public ApiLogSetting(ILogger<App> logger)
    {
        _logger = logger;
    }

    public string Title { get; set; }

    public bool EnableApiRequestLog { get; set; } = true;

    public bool DisableResponseLogout { get; set; }
    public string ConnectionId { get; set; }

    public string Path { get; set; }

    public string HttpMethod { get; set; }

    /// <summary>
    /// 请求参数
    /// </summary>
    public object Parameters { get; set; }

    public object Response { get; set; }

    public bool LogHandled { get; set; }

    public bool IsGlobal { get; set; }

    public int UserId { get; set; }

    public void Log()
    {
        if (LogHandled)
        {
            return;
        }

        if (!DisableResponseLogout)
        {
            var formattedParams = JsonConvert.SerializeObject(Parameters, Formatting.Indented);
            var formattedResponse = JsonConvert.SerializeObject(Response, Formatting.Indented);
            _logger.LogInformation("\r\nConnectionId:{connectionId} UserId:{userId} {httpMethod} {path}\r\nRequest:{params}\r\nResponse:{response}", ConnectionId, UserId, HttpMethod, Path, formattedParams, formattedResponse);
        }
        else
        {
            var formattedParams = JsonConvert.SerializeObject(Parameters, Formatting.Indented);
            _logger.LogInformation("\r\nConnectionId:{connectionId} UserId:{userId} {httpMethod} {path}\r\nRequest:{params}", ConnectionId, UserId, HttpMethod, Path, formattedParams);
        }

        LogHandled = true;
    }

    public void Log(bool disableResponseLogout)
    {
        using (LogContext.PushProperty("Prefix", $"[{Title}]"))
        {
            DisableResponseLogout = disableResponseLogout;
            Log();
        }
    }
}