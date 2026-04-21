using Kurisu.Extensions.EventBus.Abstractions;

namespace Kurisu.Extensions.EventBus.Defaults;

public class DefaultEventBusUniqueCodeGenerator : IEventBusUniqueCodeGenerator
{
    public string GenerateUniqueCode()
    {
        return Guid.NewGuid().ToString();
    }
}
