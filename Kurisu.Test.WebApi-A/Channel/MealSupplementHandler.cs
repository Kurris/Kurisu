
using Kurisu.AspNetCore.EventBus.Abstractions;
using Kurisu.AspNetCore.EventBus.Abstractions.Handler;
using Kurisu.SqlSugar.Services;

namespace Dlhis.External.MealSupplement.Channel;

public class MealSupplementHandler : IAsyncChannelHandler<MealSupplementMessage>
{
    private readonly ILogger<MealSupplementHandler> _logger;

    public MealSupplementHandler(ILogger<MealSupplementHandler> logger)
    {
        _logger = logger;
    }

    public async Task InvokeAsync(IServiceProvider serviceProvider, MealSupplementMessage message)
    {
        var db = serviceProvider.GetService<IDbContext>();
        _logger.LogInformation("MealSupplementHandler");
    }
}

public class MealSupplementMessage : IAsyncChannelMessage
{
}
