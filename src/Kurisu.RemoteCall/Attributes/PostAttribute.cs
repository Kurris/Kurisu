using Kurisu.RemoteCall.Attributes.HelpMethods;

namespace Kurisu.RemoteCall.Attributes
{
    /// <summary>
    /// post (compatibility attribute in root Attributes namespace)
    /// </summary>
    public sealed class PostAttribute : BaseHttpMethodAttribute
    {
        public PostAttribute() : this(string.Empty)
        {
        }

        public PostAttribute(string template, string contentType = "application/json", bool asUrlencodedFormat = false)
        {
            Template = template;
            ContentType = contentType;
            AsUrlencodedFormat = asUrlencodedFormat;

            if (string.Equals(ContentType, "application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase))
            {
                AsUrlencodedFormat = true;
            }
        }

        public string ContentType { get; private set; }

        /// <summary>
        /// 表示参数是否以 <c>arg1=val1&amp;arg2=val2</c> 的格式编码（并非真实表单提交）
        /// </summary>
        public bool AsUrlencodedFormat { get; private set; }

        public override string Template { get; }

        public override HttpMethod HttpMethod => HttpMethod.Post;
    }
}
