namespace AIService.Infrastructure.GrpcClients;

/// <summary>
///     城市服务 gRPC 客户端接口
/// </summary>
public interface ICityGrpcClient
{
    /// <summary>
    ///     获取城市图片URL
    /// </summary>
    /// <param name="cityId">城市ID</param>
    /// <returns>城市图片URL，优先返回横屏图片，如果没有则返回竖屏图片或主图片</returns>
    Task<string?> GetCityImageAsync(Guid cityId);
    
    /// <summary>
    ///     获取城市基本信息
    /// </summary>
    /// <param name="cityId">城市ID</param>
    /// <returns>城市信息</returns>
    Task<CityInfo?> GetCityInfoAsync(Guid cityId);
}

/// <summary>
///     城市信息 DTO
/// </summary>
public class CityInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? NameEn { get; set; }
    public string Country { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? PortraitImageUrl { get; set; }
    public List<string>? LandscapeImageUrls { get; set; }
    
    /// <summary>
    ///     获取最佳图片URL（优先横屏图片）
    /// </summary>
    public string? GetBestImageUrl()
    {
        // 优先返回横屏图片的第一张
        if (LandscapeImageUrls != null && LandscapeImageUrls.Count > 0)
        {
            return LandscapeImageUrls[0];
        }
        
        // 其次返回竖屏封面图
        if (!string.IsNullOrEmpty(PortraitImageUrl))
        {
            return PortraitImageUrl;
        }
        
        // 最后返回主图片
        return ImageUrl;
    }
}
