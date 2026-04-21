

namespace Kurisu.Extensions.EventBus.Abstractions;

public interface IEventBusUniqueCodeGenerator
{
    /// <summary>
    /// 生成唯一编码
    /// </summary>
    /// <returns></returns>
    public string GenerateUniqueCode();
}
