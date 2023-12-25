using Kurisu.Test.Framework.DI.Named.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Test.Framework.DI.Named;

[Service("email", Lifetime = ServiceLifetime.Singleton)]
public class EmailSendMessage : ISendMessage
{
    public int Count { get; set; } = 1;

    public string Send()
    {
        return "email" + Count++;
    }
}