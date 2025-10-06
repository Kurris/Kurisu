using Kurisu.AspNetCore.Abstractions.DependencyInjection;
using Kurisu.Test.Framework.DependencyInjection.Named.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Test.Framework.DependencyInjection.Named;

[DiInject("email", Lifetime = ServiceLifetime.Singleton)]
public class EmailSendMessage : ISendMessage
{
    public int Count { get; set; } = 1;

    public string Send()
    {
        return "email" + Count++;
    }
}