namespace Kurisu.AspNetCore.Abstractions.DataAccess.Contract.Field;

/// <summary>
/// 图像url
/// </summary>
public interface IImageUrl
{
    /// <summary>
    /// 图像url
    /// </summary>
    public string ImageUrl { get; set; }
}

public interface IImageUrls
{
    /// <summary>
    /// 图像url
    /// </summary>
    public string ImageUrls { get; set; }
}