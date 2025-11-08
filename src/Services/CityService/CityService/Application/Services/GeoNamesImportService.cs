using System.Text.Json;
using System.Web;
using CityService.Application.DTOs;
using CityService.Domain.Entities;
using CityService.Domain.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CityService.Application.Services;

/// <summary>
/// GeoNames 数据导入服务实现
/// </summary>
public class GeoNamesImportService : IGeoNamesImportService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IGeoNamesCityRepository _geoNamesCityRepository;
    private readonly ILogger<GeoNamesImportService> _logger;
    private readonly string _geoNamesUsername;
    private const string GeoNamesBaseUrl = "http://api.geonames.org";

    public GeoNamesImportService(
        IHttpClientFactory httpClientFactory,
        IGeoNamesCityRepository geoNamesCityRepository,
        IConfiguration configuration,
        ILogger<GeoNamesImportService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _geoNamesCityRepository = geoNamesCityRepository;
        _logger = logger;
        _geoNamesUsername = configuration["GeoNames:Username"] 
            ?? throw new InvalidOperationException("GeoNames username not configured");
    }

    /// <summary>
    /// 从 GeoNames 导入城市数据
    /// </summary>
    public async Task<GeoNamesImportResult> ImportCitiesAsync(
        GeoNamesImportOptions options, 
        CancellationToken cancellationToken = default)
    {
        var result = new GeoNamesImportResult
        {
            StartTime = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("开始从 GeoNames 导入城市数据，最小人口: {MinPopulation}", options.MinPopulation);

            // 获取所有符合条件的城市
            var geoNamesCities = await FetchCitiesFromGeoNamesAsync(options, cancellationToken);
            result.TotalProcessed = geoNamesCities.Count;

            _logger.LogInformation("从 GeoNames 获取到 {Count} 个城市", geoNamesCities.Count);

            // 分批处理
            var batches = geoNamesCities
                .Select((city, index) => new { city, index })
                .GroupBy(x => x.index / options.BatchSize)
                .Select(g => g.Select(x => x.city).ToList())
                .ToList();

            foreach (var batch in batches)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogWarning("导入操作被取消");
                    break;
                }

                await ProcessBatchAsync(batch, options, result, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导入城市数据时发生错误");
            result.Errors.Add($"导入失败: {ex.Message}");
        }
        finally
        {
            result.EndTime = DateTime.UtcNow;
            _logger.LogInformation(
                "导入完成。总计: {Total}, 成功: {Success}, 跳过: {Skipped}, 失败: {Failed}, 耗时: {Duration}",
                result.TotalProcessed, result.SuccessCount, result.SkippedCount, result.FailedCount, result.Duration);
        }

        return result;
    }

    /// <summary>
    /// 搜索 GeoNames 城市 (预览)
    /// </summary>
    public async Task<List<DTOs.GeoNamesCity>> SearchCitiesAsync(string query, int maxRows = 10)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var url = $"{GeoNamesBaseUrl}/searchJSON?" +
                      $"q={HttpUtility.UrlEncode(query)}&" +
                      $"maxRows={maxRows}&" +
                      $"featureClass=P&" +
                      $"username={_geoNamesUsername}";

            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var searchResult = JsonSerializer.Deserialize<GeoNamesSearchResponse>(content);

            return searchResult?.Geonames ?? new List<DTOs.GeoNamesCity>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜索 GeoNames 城市失败: {Query}", query);
            throw;
        }
    }

    /// <summary>
    /// 根据城市名和国家获取 GeoNames 信息
    /// </summary>
    public async Task<DTOs.GeoNamesCity?> GetCityByNameAsync(string cityName, string countryCode)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var url = $"{GeoNamesBaseUrl}/searchJSON?" +
                      $"q={HttpUtility.UrlEncode(cityName)}&" +
                      $"country={countryCode}&" +
                      $"maxRows=1&" +
                      $"featureClass=P&" +
                      $"username={_geoNamesUsername}";

            var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var searchResult = JsonSerializer.Deserialize<GeoNamesSearchResponse>(content);

            return searchResult?.Geonames?.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取城市信息失败: {CityName}, {CountryCode}", cityName, countryCode);
            return null;
        }
    }

    #region Private Methods

    /// <summary>
    /// 从 GeoNames 获取城市列表
    /// </summary>
    private async Task<List<DTOs.GeoNamesCity>> FetchCitiesFromGeoNamesAsync(
        GeoNamesImportOptions options,
        CancellationToken cancellationToken)
    {
        var allCities = new List<DTOs.GeoNamesCity>();
        var client = _httpClientFactory.CreateClient();

        // 如果指定了国家列表
        var countries = options.CountryCodes ?? GetDefaultCountryCodes();
        
        _logger.LogInformation("准备从 {Count} 个国家获取城市数据: {Countries}", 
            countries.Count, string.Join(", ", countries));

        foreach (var countryCode in countries)
        {
            if (cancellationToken.IsCancellationRequested) break;

            try
            {
                _logger.LogInformation("开始获取 {Country} 的城市数据", countryCode);
                
                var startRow = 0;
                var maxRows = 1000; // GeoNames 每次最多返回 1000 条
                var hasMore = true;

                while (hasMore && !cancellationToken.IsCancellationRequested)
                {
                    var url = BuildSearchUrl(countryCode, options, startRow, maxRows);
                    _logger.LogInformation("请求 GeoNames API: {Url}", url);
                    
                    var response = await client.GetAsync(url, cancellationToken);
                    response.EnsureSuccessStatusCode();

                    var content = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogDebug("GeoNames 响应: {Content}", content.Substring(0, Math.Min(500, content.Length)));
                    
                    var searchResult = JsonSerializer.Deserialize<GeoNamesSearchResponse>(content);

                    if (searchResult?.Geonames != null && searchResult.Geonames.Any())
                    {
                        // 根据最小人口过滤城市
                        var filteredCities = searchResult.Geonames
                            .Where(c => c.Population >= options.MinPopulation)
                            .ToList();
                        
                        allCities.AddRange(filteredCities);
                        
                        if (searchResult.Geonames.Count < maxRows)
                        {
                            hasMore = false;
                        }
                        else
                        {
                            startRow += maxRows;
                        }

                        _logger.LogInformation(
                            "从 {Country} 获取 {Count} 个城市 (过滤后 {Filtered} 个，人口 >= {MinPop})，总计: {Total}", 
                            countryCode, searchResult.Geonames.Count, filteredCities.Count, options.MinPopulation, allCities.Count);
                    }
                    else
                    {
                        _logger.LogWarning("从 {Country} 未获取到城市数据", countryCode);
                        hasMore = false;
                    }

                    // 避免超过 API 限制
                    await Task.Delay(200, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取 {Country} 城市数据失败", countryCode);
            }
        }

        return allCities;
    }

    /// <summary>
    /// 构建搜索 URL
    /// </summary>
    private string BuildSearchUrl(string countryCode, GeoNamesImportOptions options, int startRow, int maxRows)
    {
        // GeoNames API 不直接支持自定义人口过滤
        // 我们先获取所有城市,然后在代码中过滤人口
        
        var url = $"{GeoNamesBaseUrl}/searchJSON?" +
                  $"country={countryCode}&" +
                  $"featureClass={options.FeatureClass}&" +
                  $"maxRows={maxRows}&" +
                  $"startRow={startRow}&" +
                  $"username={_geoNamesUsername}";

        if (options.FeatureCodes.Any())
        {
            url += $"&featureCode={string.Join(",", options.FeatureCodes)}";
        }

        return url;
    }

    /// <summary>
    /// 处理一批城市
    /// </summary>
    private async Task ProcessBatchAsync(
        List<DTOs.GeoNamesCity> batch,
        GeoNamesImportOptions options,
        GeoNamesImportResult result,
        CancellationToken cancellationToken)
    {
        foreach (var geoCity in batch)
        {
            if (cancellationToken.IsCancellationRequested) break;

            try
            {
                await ProcessSingleCityAsync(geoCity, options, result);
            }
            catch (Exception ex)
            {
                result.FailedCount++;
                result.Errors.Add($"处理 {geoCity.Name} 失败: {ex.Message}");
                _logger.LogError(ex, "处理城市 {CityName} 失败", geoCity.Name);
            }
        }
    }

    /// <summary>
    /// 处理单个城市
    /// </summary>
    private async Task ProcessSingleCityAsync(
        DTOs.GeoNamesCity geoCity,
        GeoNamesImportOptions options,
        GeoNamesImportResult result)
    {
        try
        {
            // 映射到实体
            var entity = MapToGeoNamesCityEntity(geoCity);
            
            // Upsert (如果存在则更新,否则创建)
            await _geoNamesCityRepository.UpsertAsync(entity);
            result.SuccessCount++;
            _logger.LogInformation("导入城市: {Name}, {Country}", geoCity.Name, geoCity.CountryName);
        }
        catch (Exception ex)
        {
            result.FailedCount++;
            var errorMsg = $"处理 {geoCity.Name} 失败: {ex.Message}";
            result.Errors.Add(errorMsg);
            _logger.LogError(ex, "处理城市失败: {Name}", geoCity.Name);
        }
    }

    /// <summary>
    /// 获取国家代码
    /// </summary>
    private string GetCountryCode(string countryName)
    {
        // 简单的国家名到代码映射
        var countryMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "China", "CN" },
            { "United States", "US" },
            { "Thailand", "TH" },
            { "Indonesia", "ID" },
            { "Portugal", "PT" },
            { "Spain", "ES" },
            { "Mexico", "MX" },
            { "Vietnam", "VN" },
            { "Japan", "JP" },
            { "South Korea", "KR" },
            // 添加更多映射...
        };

        return countryMap.TryGetValue(countryName, out var code) ? code : "US";
    }

    /// <summary>
    /// 获取默认的国家代码列表（数字游民热门国家）
    /// </summary>
    private List<string> GetDefaultCountryCodes()
    {
        return new List<string>
        {
            "TH", // 泰国
            "ID", // 印度尼西亚
            "PT", // 葡萄牙
            "ES", // 西班牙
            "MX", // 墨西哥
            "VN", // 越南
            "CN", // 中国
            "JP", // 日本
            "KR", // 韩国
            "MY", // 马来西亚
            "PH", // 菲律宾
            "TW", // 台湾
            "SG", // 新加坡
            "AE", // 阿联酋
            "TR", // 土耳其
            "GR", // 希腊
            "IT", // 意大利
            "FR", // 法国
            "GB", // 英国
            "DE", // 德国
            "NL", // 荷兰
            "CZ", // 捷克
            "PL", // 波兰
            "HU", // 匈牙利
            "US", // 美国
            "CA", // 加拿大
            "AU", // 澳大利亚
            "NZ", // 新西兰
            "BR", // 巴西
            "AR", // 阿根廷
            "CO", // 哥伦比亚
            "CR", // 哥斯达黎加
            "ZA", // 南非
            "MA", // 摩洛哥
            "EG", // 埃及
        };
    }

    /// <summary>
    /// 映射 DTO 到实体
    /// </summary>
    private Domain.Entities.GeoNamesCity MapToGeoNamesCityEntity(DTOs.GeoNamesCity dto)
    {
        return new Domain.Entities.GeoNamesCity
        {
            Id = Guid.NewGuid(),
            GeonameId = dto.GeonameId,
            Name = dto.Name,
            AsciiName = dto.AsciiName,
            AlternateNames = null, // DTO 中没有该字段
            Latitude = double.TryParse(dto.Lat, out var lat) ? lat : null,
            Longitude = double.TryParse(dto.Lng, out var lng) ? lng : null,
            FeatureClass = dto.FeatureClass,
            FeatureCode = dto.FeatureCode,
            CountryCode = dto.CountryCode,
            CountryName = dto.CountryName,
            Admin1Code = null,
            Admin1Name = dto.AdminName1,
            Admin2Code = null,
            Admin2Name = null,
            Admin3Code = null,
            Admin4Code = null,
            Population = dto.Population > 0 ? dto.Population : null,
            Elevation = null,
            Dem = null,
            Timezone = dto.Timezone?.TimeZoneId,
            ModificationDate = null,
            SyncedToCities = false,
            CityId = null,
            ImportedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Notes = null
        };
    }

    #endregion
}
