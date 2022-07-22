using System.Collections.Generic;
using Kurisu.Channel.Abstractions;

namespace WebApiDemo.Channel.Messages
{
    public class BobMessage : IChannelMessage
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public IEnumerable<string> Claims { get; set; }
    }
}