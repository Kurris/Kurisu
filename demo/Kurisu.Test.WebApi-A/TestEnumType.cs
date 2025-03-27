using Kurisu.AspNetCore.CustomClass;

namespace Kurisu.Test.WebApi_A;

public enum TestEnumType
{
    [Lang("Wait111", "en")]
    [Lang("等待")]
    Wait = 0,

    [Lang("Finished", "en")]
    [Lang("完成")]
    Finished = 99
}