using System.Text.Json;

namespace Kurisu.AspNetCore.Abstractions.State;

public interface ICopyable<out T>
{
    public T Copy()
    {
        var json = JsonSerializer.Serialize(this);
        return JsonSerializer.Deserialize<T>(json);
    }
}