namespace Kurisu.UnifyResultAndValidation
{
    /// <summary>
    /// api result
    /// </summary>
    public interface IApiResult
    {
        IApiResult GetDefaultValidateResult<T>(T apiResult);
        IApiResult GetDefaultSuccessResult<T>(T apiResult);
        IApiResult GetDefaultErrorResult<T>(T apiResult);
    }
}