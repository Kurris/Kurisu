using Kurisu.EFSharding.Sharding.Parsers.Abstractions;

namespace Kurisu.EFSharding.Extensions;

public static class CompileExtension
{
    /// <summary>
    /// 是否存在自定义查询
    /// </summary>
    /// <param name="prepareParseResult"></param>
    /// <returns></returns>
    public static bool HasCustomerQuery(this IPrepareParseResult prepareParseResult)
    {
        //compileParameter.ReadOnly().HasValue || compileParameter.GetAsRoute() != null;
        return prepareParseResult.GetAsRoute() != null;
    }
}