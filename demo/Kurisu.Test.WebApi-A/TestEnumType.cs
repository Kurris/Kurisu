using System.ComponentModel;
using Kurisu.AspNetCore.CustomClass;

namespace Kurisu.Test.WebApi_A
{
    public enum TestEnumType
    {
        [DescriptionEn("Wait")]
        [Description("等待")]
        Wait = 0,

        [DescriptionEn("Finished")]
        [Description("完成")]
        Finished = 99
    }
}