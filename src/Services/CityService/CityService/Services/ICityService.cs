using CityService.DTOs;

namespace CityService.Services;

public interface ICityService
{
    Task<IEnumerable<CityDto>> GetAllCitiesAsync(int pageNumber, int pageSize);
    Task<CityDto?> GetCityByIdAsync(Guid id);
    Task<IEnumerable<CityDto>> SearchCitiesAsync(CitySearchDto searchDto);
    Task<CityDto> CreateCityAsync(CreateCityDto createCityDto, Guid userId);
    Task<CityDto?> UpdateCityAsync(Guid id, UpdateCityDto updateCityDto, Guid userId);
    Task<bool> DeleteCityAsync(Guid id);
    Task<int> GetTotalCountAsync();
    Task<IEnumerable<CityDto>> GetRecommendedCitiesAsync(int count);
    Task<CityStatisticsDto?> GetCityStatisticsAsync(Guid id);
}
