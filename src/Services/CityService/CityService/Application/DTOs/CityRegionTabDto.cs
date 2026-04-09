namespace CityService.Application.DTOs;

public class CityRegionTabDto
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int CityCount { get; set; }
    public int DisplayOrder { get; set; }
}