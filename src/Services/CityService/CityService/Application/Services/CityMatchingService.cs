using CityService.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace CityService.Application.Services;

/// <summary>
///     城市匹配服务实现 - 多策略匹配
/// </summary>
public class CityMatchingService : ICityMatchingService
{
    private readonly ICityRepository _cityRepository;
    private readonly IGeocodingService _geocodingService;
    private readonly ILogger<CityMatchingService> _logger;

    // 匹配阈值
    private const double ExactNameConfidence = 1.0;
    private const double NameAndCoordinateConfidence = 0.9;
    private const double CoordinateOnlyConfidence = 0.7;
    private const double ReverseGeocodeConfidence = 0.6;
    private const double MaxDistanceKm = 50.0; // 最大匹配距离50公里

    public CityMatchingService(
        ICityRepository cityRepository,
        IGeocodingService geocodingService,
        ILogger<CityMatchingService> logger)
    {
        _cityRepository = cityRepository;
        _geocodingService = geocodingService;
        _logger = logger;
    }

    /// <summary>
    ///     根据经纬度和城市名称匹配城市
    ///     匹配策略优先级：ExactName → NameAndCoordinate → CoordinateOnly → ReverseGeocode
    /// </summary>
    public async Task<CityMatchResult> MatchCityAsync(
        CityMatchRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "开始城市匹配: Lat={Latitude}, Lng={Longitude}, CityName={CityName}, CityNameEn={CityNameEn}",
            request.Latitude, request.Longitude, request.CityName, request.CityNameEn);

        try
        {
            // 策略1: 名称精确匹配
            var exactMatch = await TryExactNameMatchAsync(request, cancellationToken);
            if (exactMatch.IsMatched)
            {
                _logger.LogInformation("名称精确匹配成功: CityId={CityId}", exactMatch.CityId);
                return exactMatch;
            }

            // 策略2: 名称 + 坐标组合匹配
            var nameAndCoordMatch = await TryNameAndCoordinateMatchAsync(request, cancellationToken);
            if (nameAndCoordMatch.IsMatched)
            {
                _logger.LogInformation("名称+坐标匹配成功: CityId={CityId}", nameAndCoordMatch.CityId);
                return nameAndCoordMatch;
            }

            // 策略3: 基于坐标距离匹配
            var coordMatch = await TryCoordinateOnlyMatchAsync(request, cancellationToken);
            if (coordMatch.IsMatched)
            {
                _logger.LogInformation("坐标距离匹配成功: CityId={CityId}, Distance={Distance}km",
                    coordMatch.CityId, coordMatch.DistanceKm);
                return coordMatch;
            }

            // 策略4: 反向地理编码匹配
            var reverseGeocodeMatch = await TryReverseGeocodeMatchAsync(request, cancellationToken);
            if (reverseGeocodeMatch.IsMatched)
            {
                _logger.LogInformation("反向地理编码匹配成功: CityId={CityId}", reverseGeocodeMatch.CityId);
                return reverseGeocodeMatch;
            }

            _logger.LogWarning("城市匹配失败，未找到匹配的城市");
            return new CityMatchResult
            {
                IsMatched = false,
                MatchMethod = CityMatchMethod.None,
                Confidence = 0,
                ErrorMessage = "未找到匹配的城市"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "城市匹配过程中发生错误");
            return new CityMatchResult
            {
                IsMatched = false,
                MatchMethod = CityMatchMethod.None,
                Confidence = 0,
                ErrorMessage = $"匹配错误: {ex.Message}"
            };
        }
    }

    /// <summary>
    ///     策略1: 名称精确匹配
    /// </summary>
    private async Task<CityMatchResult> TryExactNameMatchAsync(
        CityMatchRequest request,
        CancellationToken cancellationToken)
    {
        // 尝试用英文名匹配
        if (!string.IsNullOrWhiteSpace(request.CityNameEn))
        {
            var cities = await _cityRepository.SearchByNameAsync(
                request.CityNameEn,
                request.CountryCode,
                cancellationToken);

            var exactMatch = cities.FirstOrDefault(c =>
                string.Equals(c.NameEn, request.CityNameEn, StringComparison.OrdinalIgnoreCase));

            if (exactMatch != null)
            {
                return new CityMatchResult
                {
                    IsMatched = true,
                    CityId = exactMatch.Id.ToString(),
                    CityName = exactMatch.Name,
                    CityNameEn = exactMatch.NameEn,
                    CountryName = exactMatch.Country,
                    MatchMethod = CityMatchMethod.ExactName,
                    Confidence = ExactNameConfidence
                };
            }
        }

        // 尝试用本地名匹配
        if (!string.IsNullOrWhiteSpace(request.CityName))
        {
            var cities = await _cityRepository.SearchByNameAsync(
                request.CityName,
                request.CountryCode,
                cancellationToken);

            var exactMatch = cities.FirstOrDefault(c =>
                string.Equals(c.Name, request.CityName, StringComparison.OrdinalIgnoreCase));

            if (exactMatch != null)
            {
                return new CityMatchResult
                {
                    IsMatched = true,
                    CityId = exactMatch.Id.ToString(),
                    CityName = exactMatch.Name,
                    CityNameEn = exactMatch.NameEn,
                    CountryName = exactMatch.Country,
                    MatchMethod = CityMatchMethod.ExactName,
                    Confidence = ExactNameConfidence
                };
            }
        }

        return new CityMatchResult { IsMatched = false };
    }

    /// <summary>
    ///     策略2: 名称 + 坐标组合匹配（名称模糊匹配 + 坐标在合理范围内）
    /// </summary>
    private async Task<CityMatchResult> TryNameAndCoordinateMatchAsync(
        CityMatchRequest request,
        CancellationToken cancellationToken)
    {
        var searchName = request.CityNameEn ?? request.CityName;
        if (string.IsNullOrWhiteSpace(searchName))
            return new CityMatchResult { IsMatched = false };

        var cities = await _cityRepository.SearchByNameAsync(
            searchName,
            request.CountryCode,
            cancellationToken);

        foreach (var city in cities)
        {
            // 跳过没有坐标的城市
            if (!city.Latitude.HasValue || !city.Longitude.HasValue)
                continue;

            var distance = CalculateDistanceKm(
                request.Latitude, request.Longitude,
                city.Latitude.Value, city.Longitude.Value);

            // 名称相似且坐标在50公里范围内
            if (distance <= MaxDistanceKm)
            {
                return new CityMatchResult
                {
                    IsMatched = true,
                    CityId = city.Id.ToString(),
                    CityName = city.Name,
                    CityNameEn = city.NameEn,
                    CountryName = city.Country,
                    MatchMethod = CityMatchMethod.NameAndCoordinate,
                    DistanceKm = distance,
                    Confidence = NameAndCoordinateConfidence
                };
            }
        }

        return new CityMatchResult { IsMatched = false };
    }

    /// <summary>
    ///     策略3: 基于坐标距离匹配（查找最近的城市）
    /// </summary>
    private async Task<CityMatchResult> TryCoordinateOnlyMatchAsync(
        CityMatchRequest request,
        CancellationToken cancellationToken)
    {
        var nearestCity = await _cityRepository.FindNearestCityAsync(
            request.Latitude,
            request.Longitude,
            MaxDistanceKm,
            cancellationToken);

        if (nearestCity != null && nearestCity.Latitude.HasValue && nearestCity.Longitude.HasValue)
        {
            var distance = CalculateDistanceKm(
                request.Latitude, request.Longitude,
                nearestCity.Latitude.Value, nearestCity.Longitude.Value);

            return new CityMatchResult
            {
                IsMatched = true,
                CityId = nearestCity.Id.ToString(),
                CityName = nearestCity.Name,
                CityNameEn = nearestCity.NameEn,
                CountryName = nearestCity.Country,
                MatchMethod = CityMatchMethod.CoordinateOnly,
                DistanceKm = distance,
                Confidence = CoordinateOnlyConfidence
            };
        }

        return new CityMatchResult { IsMatched = false };
    }

    /// <summary>
    ///     策略4: 反向地理编码匹配（调用高德/Google API获取城市名，再匹配）
    /// </summary>
    private async Task<CityMatchResult> TryReverseGeocodeMatchAsync(
        CityMatchRequest request,
        CancellationToken cancellationToken)
    {
        var geocodeResult = await _geocodingService.ReverseGeocodeAsync(
            request.Latitude,
            request.Longitude,
            cancellationToken);

        if (geocodeResult == null || string.IsNullOrWhiteSpace(geocodeResult.CityName))
            return new CityMatchResult { IsMatched = false };

        // 用反向地理编码得到的城市名再次搜索
        var cities = await _cityRepository.SearchByNameAsync(
            geocodeResult.CityName,
            geocodeResult.CountryCode,
            cancellationToken);

        var matchedCity = cities.FirstOrDefault(c => c.Latitude.HasValue && c.Longitude.HasValue);
        if (matchedCity != null)
        {
            var distance = CalculateDistanceKm(
                request.Latitude, request.Longitude,
                matchedCity.Latitude!.Value, matchedCity.Longitude!.Value);

            return new CityMatchResult
            {
                IsMatched = true,
                CityId = matchedCity.Id.ToString(),
                CityName = matchedCity.Name,
                CityNameEn = matchedCity.NameEn,
                CountryName = matchedCity.Country,
                MatchMethod = CityMatchMethod.ReverseGeocode,
                DistanceKm = distance,
                Confidence = ReverseGeocodeConfidence
            };
        }

        return new CityMatchResult { IsMatched = false };
    }

    /// <summary>
    ///     计算两点之间的距离（Haversine公式）
    /// </summary>
    private static double CalculateDistanceKm(
        double lat1, double lon1,
        double lat2, double lon2)
    {
        const double R = 6371; // 地球半径（公里）

        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return R * c;
    }

    private static double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180;
    }
}
