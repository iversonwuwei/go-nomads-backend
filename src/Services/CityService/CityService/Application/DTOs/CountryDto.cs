namespace CityService.Application.DTOs;

public class CountryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NameZh { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? CodeAlpha3 { get; set; }
    public string? Continent { get; set; }
    public string? FlagUrl { get; set; }
    public string? CallingCode { get; set; }
    public bool IsActive { get; set; }
}
