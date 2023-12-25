namespace Kurisu.AspNetCore.EventBus.Abstractions;

public interface IEventBusResult<T>
{
    public T ReturnResult { get; set; }
}