using Kurisu.Extensions.EventBus.Abstractions;

namespace Kurisu.Extensions.EventBus.Defaults;

/// <summary>
/// 默认消息 code 生成器，使用 Guid 作为唯一标识。
/// </summary>
public class DefaultEventBusUniqueCodeGenerator : IEventBusUniqueCodeGenerator
{
    public string GenerateUniqueCode()
    {
        return Guid.NewGuid().ToString();
    }
}
