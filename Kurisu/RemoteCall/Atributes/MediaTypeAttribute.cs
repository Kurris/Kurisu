using System;

namespace Kurisu.RemoteCall.Atributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class MediaTypeAttribute : Attribute
    {
        private readonly string _type;

        public MediaTypeAttribute(string type)
        {
            _type = type;
        }

        public string Type => _type;
    }
}
