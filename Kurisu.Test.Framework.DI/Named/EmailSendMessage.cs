using Kurisu.Test.Framework.DI.Named.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Kurisu.Test.Framework.DI.Named
{
    [Register("email")]
    public class EmailSendMessage : ISendMessage
    {
        public string Send()
        {
            return "email";
        }
    }
}