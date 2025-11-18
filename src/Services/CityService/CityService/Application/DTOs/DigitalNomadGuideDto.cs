namespace CityService.Application.DTOs;

/// <summary>
///     数字游民指南DTO
/// </summary>
public class DigitalNomadGuideDto
{
    public string Id { get; set; } = string.Empty;
    public string CityId { get; set; } = string.Empty;
    public string CityName { get; set; } = string.Empty;
    public string Overview { get; set; } = string.Empty;
    public VisaInfoDto VisaInfo { get; set; } = new();
    public List<BestAreaDto> BestAreas { get; set; } = new();
    public List<string> WorkspaceRecommendations { get; set; } = new();
    public List<string> Tips { get; set; } = new();
    public Dictionary<string, string> EssentialInfo { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
///     签证信息DTO
/// </summary>
public class VisaInfoDto
{
    public string Type { get; set; } = string.Empty;
    public int Duration { get; set; }
    public string Requirements { get; set; } = string.Empty;
    public double Cost { get; set; }
    public string Process { get; set; } = string.Empty;
}

/// <summary>
///     最佳区域DTO
/// </summary>
public class BestAreaDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double EntertainmentScore { get; set; }
    public string EntertainmentDescription { get; set; } = string.Empty;
    public double TourismScore { get; set; }
    public string TourismDescription { get; set; } = string.Empty;
    public double EconomyScore { get; set; }
    public string EconomyDescription { get; set; } = string.Empty;
    public double CultureScore { get; set; }
    public string CultureDescription { get; set; } = string.Empty;
}

/// <summary>
///     保存指南请求DTO
/// </summary>
public class SaveDigitalNomadGuideRequest
{
    public string CityId { get; set; } = string.Empty;
    public string CityName { get; set; } = string.Empty;
    public string Overview { get; set; } = string.Empty;
    public VisaInfoDto VisaInfo { get; set; } = new();
    public List<BestAreaDto> BestAreas { get; set; } = new();
    public List<string> WorkspaceRecommendations { get; set; } = new();
    public List<string> Tips { get; set; } = new();
    public Dictionary<string, string> EssentialInfo { get; set; } = new();
}