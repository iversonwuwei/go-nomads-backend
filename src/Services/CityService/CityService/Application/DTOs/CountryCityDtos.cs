namespace CityService.Application.DTOs;

public class CountryCitiesDto
{
    public string Country { get; set; } = string.Empty;
    public List<CitySummaryDto> Cities { get; set; } = new();
}

public class CitySummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Region { get; set; }
}