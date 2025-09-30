using Kurisu.AspNetCore.EventBus.Abstractions;
using Kurisu.AspNetCore.EventBus.Abstractions.Handler;

namespace Kurisu.Test.WebApi_A.Channels;

public class MealSupplementHandler : CommonChannelHandler<MealSupplementMessage>
{
    /// <inheritdoc />
    public MealSupplementHandler(ILogger<CommonChannelHandler<MealSupplementMessage>> logger) : base(logger)
    {
    }

    /// <inheritdoc />
    protected override async Task InvokeAsync(MealSupplementMessage message)
    {
    }
}

public class MealSupplementMessage : IAsyncChannelMessage
{
}