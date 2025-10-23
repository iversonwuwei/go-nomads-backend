using CityService.Models;
using CityService.Repositories;

namespace CityService.Services;

/// <summary>
/// 地理数据初始化服务
/// </summary>
public class GeographyDataSeeder
{
    private readonly ICountryRepository _countryRepository;
    private readonly IProvinceRepository _provinceRepository;
    private readonly ICityRepository _cityRepository;
    private readonly ILogger<GeographyDataSeeder> _logger;

    public GeographyDataSeeder(
        ICountryRepository countryRepository,
        IProvinceRepository provinceRepository,
        ICityRepository cityRepository,
        ILogger<GeographyDataSeeder> logger)
    {
        _countryRepository = countryRepository;
        _provinceRepository = provinceRepository;
        _cityRepository = cityRepository;
        _logger = logger;
    }

    /// <summary>
    /// 初始化中国省市数据
    /// </summary>
    public async Task<SeedResult> SeedChinaProvincesAndCitiesAsync(List<ProvinceData> provinceDataList)
    {
        var result = new SeedResult();
        
        try
        {
            // 1. 获取或创建中国
            var china = await _countryRepository.GetCountryByCodeAsync("CN");
            if (china == null)
            {
                china = await _countryRepository.CreateCountryAsync(new Country
                {
                    Name = "China",
                    NameZh = "中国",
                    Code = "CN",
                    CodeAlpha3 = "CHN",
                    Continent = "Asia",
                    IsActive = true
                });
                result.CountriesCreated++;
                _logger.LogInformation("Created country: China");
            }

            // 2. 创建省份
            foreach (var provinceData in provinceDataList)
            {
                var province = new Province
                {
                    Name = provinceData.Province,
                    CountryId = china.Id,
                    IsActive = true
                };

                var createdProvince = await _provinceRepository.CreateProvinceAsync(province);
                result.ProvincesCreated++;
                _logger.LogInformation("Created province: {ProvinceName}", provinceData.Province);

                // 3. 创建该省份下的所有城市
                foreach (var cityName in provinceData.Cities)
                {
                    try
                    {
                        var city = new City
                        {
                            Name = cityName,
                            Country = "China",
                            CountryId = china.Id,
                            ProvinceId = createdProvince.Id,
                            Region = provinceData.Province,
                            IsActive = true
                        };

                        await _cityRepository.CreateAsync(city);
                        result.CitiesCreated++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to create city: {CityName}", cityName);
                        result.CitiesFailed++;
                    }
                }
            }

            result.Success = true;
            _logger.LogInformation(
                "Data seeding completed. Countries: {Countries}, Provinces: {Provinces}, Cities: {Cities}, Failed: {Failed}",
                result.CountriesCreated, result.ProvincesCreated, result.CitiesCreated, result.CitiesFailed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding China provinces and cities data");
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    /// <summary>
    /// 批量导入国家数据
    /// </summary>
    public async Task<int> SeedCountriesAsync(List<CountryData> countryDataList)
    {
        var count = 0;
        
        foreach (var countryData in countryDataList)
        {
            try
            {
                // 检查是否已存在
                var existing = await _countryRepository.GetCountryByCodeAsync(countryData.Code);
                if (existing != null)
                {
                    _logger.LogInformation("Country already exists: {Code}", countryData.Code);
                    continue;
                }

                var country = new Country
                {
                    Name = countryData.Name,
                    NameZh = countryData.NameZh,
                    Code = countryData.Code,
                    CodeAlpha3 = countryData.CodeAlpha3,
                    Continent = countryData.Continent,
                    CallingCode = countryData.CallingCode,
                    FlagUrl = countryData.FlagUrl,
                    IsActive = true
                };

                await _countryRepository.CreateCountryAsync(country);
                count++;
                _logger.LogInformation("Created country: {Name} ({Code})", country.Name, country.Code);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create country: {Name}", countryData.Name);
            }
        }

        return count;
    }
}

public class ProvinceData
{
    public string Province { get; set; } = string.Empty;
    public List<string> Cities { get; set; } = new();
}

public class CountryData
{
    public string Name { get; set; } = string.Empty;
    public string NameZh { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? CodeAlpha3 { get; set; }
    public string? Continent { get; set; }
    public string? CallingCode { get; set; }
    public string? FlagUrl { get; set; }
}

public class SeedResult
{
    public bool Success { get; set; }
    public int CountriesCreated { get; set; }
    public int ProvincesCreated { get; set; }
    public int CitiesCreated { get; set; }
    public int CitiesFailed { get; set; }
    public string? ErrorMessage { get; set; }
}
