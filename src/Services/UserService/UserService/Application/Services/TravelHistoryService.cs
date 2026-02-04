using System.Text.Json;
using UserService.Application.DTOs;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;
using UserService.Infrastructure.Services;

namespace UserService.Application.Services;

/// <summary>
///     旅行历史服务实现
/// </summary>
public class TravelHistoryService : ITravelHistoryService
{
    private readonly ILogger<TravelHistoryService> _logger;
    private readonly ITravelHistoryRepository _travelHistoryRepository;
    private readonly ICityServiceClient _cityServiceClient;

    public TravelHistoryService(
        ITravelHistoryRepository travelHistoryRepository,
        ICityServiceClient cityServiceClient,
        ILogger<TravelHistoryService> logger)
    {
        _travelHistoryRepository = travelHistoryRepository;
        _cityServiceClient = cityServiceClient;
        _logger = logger;
    }

    public async Task<(List<TravelHistoryDto> Items, int Total)> GetUserTravelHistoryAsync(
        string userId,
        int page = 1,
        int pageSize = 20,
        bool? isConfirmed = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📋 获取用户旅行历史 - UserId: {UserId}, Page: {Page}, IsConfirmed: {IsConfirmed}",
            userId, page, isConfirmed);

        var (items, total) = await _travelHistoryRepository.GetByUserIdAsync(
            userId, page, pageSize, isConfirmed, cancellationToken);

        var dtos = items.Select(MapToDto).ToList();
        return (dtos, total);
    }

    public async Task<List<TravelHistoryDto>> GetConfirmedTravelHistoryAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📋 获取用户已确认的旅行历史 - UserId: {UserId}", userId);

        var items = await _travelHistoryRepository.GetConfirmedByUserIdAsync(userId, cancellationToken);
        return items.Select(MapToDto).ToList();
    }

    public async Task<TravelHistoryDto?> GetLatestTravelHistoryAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 获取用户最新旅行历史 - UserId: {UserId}", userId);

        // 获取最新一条已确认的旅行历史
        var (items, _) = await _travelHistoryRepository.GetByUserIdAsync(
            userId, page: 1, pageSize: 1, isConfirmed: true, cancellationToken);

        var latest = items.FirstOrDefault();
        return latest != null ? MapToDto(latest) : null;
    }

    public async Task<List<TravelHistoryDto>> GetUnconfirmedTravelHistoryAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📋 获取用户未确认的旅行历史 - UserId: {UserId}", userId);

        var items = await _travelHistoryRepository.GetUnconfirmedByUserIdAsync(userId, cancellationToken);
        return items.Select(MapToDto).ToList();
    }

    public async Task<TravelHistoryDto?> GetTravelHistoryByIdAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 获取旅行历史详情 - Id: {Id}", id);

        var item = await _travelHistoryRepository.GetByIdAsync(id, cancellationToken);
        return item != null ? MapToDto(item) : null;
    }

    public async Task<TravelHistoryDto> CreateTravelHistoryAsync(
        string userId,
        CreateTravelHistoryDto dto,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📝 创建旅行历史记录 - UserId: {UserId}, City: {City}", userId, dto.City);

        // 检查是否已存在相似记录（避免重复）
        var tolerance = TimeSpan.FromHours(24); // 24小时内的相同地点视为重复
        var exists = await _travelHistoryRepository.ExistsSimilarAsync(
            userId, dto.City, dto.Country, dto.ArrivalTime, tolerance, cancellationToken);

        if (exists)
        {
            _logger.LogWarning("⚠️ 已存在相似的旅行记录: {City}, {Country}, {ArrivalTime}",
                dto.City, dto.Country, dto.ArrivalTime);
            throw new InvalidOperationException("已存在相似的旅行记录");
        }

        // 如果没有提供 CityId，尝试自动匹配
        var cityId = dto.CityId;
        if (string.IsNullOrEmpty(cityId) && dto.Latitude.HasValue && dto.Longitude.HasValue)
        {
            cityId = await TryMatchCityAsync(dto.City, dto.Country, dto.CountryCode,
                dto.Latitude.Value, dto.Longitude.Value, cancellationToken);
        }

        var travelHistory = TravelHistory.Create(
            userId,
            dto.City,
            dto.Country,
            dto.ArrivalTime,
            dto.DepartureTime,
            dto.Latitude,
            dto.Longitude,
            dto.IsConfirmed,
            cityId
        );

        // 设置国家代码
        if (!string.IsNullOrEmpty(dto.CountryCode))
            travelHistory.CountryCode = dto.CountryCode;

        // 设置评价
        if (!string.IsNullOrEmpty(dto.Review))
            travelHistory.Review = dto.Review;

        if (dto.Rating.HasValue)
            travelHistory.Rating = dto.Rating.Value;

        // 设置照片
        if (dto.Photos != null && dto.Photos.Count > 0)
            travelHistory.Photos = JsonSerializer.Serialize(dto.Photos);

        var created = await _travelHistoryRepository.CreateAsync(travelHistory, cancellationToken);
        _logger.LogInformation("✅ 成功创建旅行历史记录: {Id}", created.Id);

        return MapToDto(created);
    }

    public async Task<List<TravelHistoryDto>> CreateBatchTravelHistoryAsync(
        string userId,
        BatchCreateTravelHistoryDto dto,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📝 批量创建旅行历史记录 - UserId: {UserId}, Count: {Count}", userId, dto.Items.Count);

        var travelHistories = new List<TravelHistory>();
        var tolerance = TimeSpan.FromHours(24);

        foreach (var item in dto.Items)
        {
            // 检查是否已存在相似记录
            var exists = await _travelHistoryRepository.ExistsSimilarAsync(
                userId, item.City, item.Country, item.ArrivalTime, tolerance, cancellationToken);

            if (exists)
            {
                _logger.LogWarning("⚠️ 跳过已存在的旅行记录: {City}, {Country}", item.City, item.Country);
                continue;
            }

            // 如果没有提供 CityId，尝试自动匹配
            var cityId = item.CityId;
            if (string.IsNullOrEmpty(cityId) && item.Latitude.HasValue && item.Longitude.HasValue)
            {
                cityId = await TryMatchCityAsync(item.City, item.Country, item.CountryCode,
                    item.Latitude.Value, item.Longitude.Value, cancellationToken);
            }

            var travelHistory = TravelHistory.Create(
                userId,
                item.City,
                item.Country,
                item.ArrivalTime,
                item.DepartureTime,
                item.Latitude,
                item.Longitude,
                item.IsConfirmed,
                cityId
            );

            if (!string.IsNullOrEmpty(item.Review))
                travelHistory.Review = item.Review;

            if (item.Rating.HasValue)
                travelHistory.Rating = item.Rating.Value;

            if (item.Photos != null && item.Photos.Count > 0)
                travelHistory.Photos = JsonSerializer.Serialize(item.Photos);

            travelHistories.Add(travelHistory);
        }

        if (travelHistories.Count == 0)
        {
            _logger.LogWarning("⚠️ 没有新的旅行记录需要创建");
            return new List<TravelHistoryDto>();
        }

        var created = await _travelHistoryRepository.CreateBatchAsync(travelHistories, cancellationToken);
        _logger.LogInformation("✅ 成功批量创建 {Count} 条旅行历史记录", created.Count);

        return created.Select(MapToDto).ToList();
    }

    public async Task<TravelHistoryDto> UpdateTravelHistoryAsync(
        string id,
        UpdateTravelHistoryDto dto,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📝 更新旅行历史记录 - Id: {Id}", id);

        var travelHistory = await _travelHistoryRepository.GetByIdAsync(id, cancellationToken);
        if (travelHistory == null)
            throw new InvalidOperationException("旅行历史记录不存在");

        // 更新照片
        string? photos = null;
        if (dto.Photos != null)
            photos = dto.Photos.Count > 0 ? JsonSerializer.Serialize(dto.Photos) : null;

        travelHistory.Update(
            dto.City,
            dto.Country,
            dto.ArrivalTime,
            dto.DepartureTime,
            dto.Latitude,
            dto.Longitude,
            dto.Review,
            dto.Rating,
            photos,
            dto.CityId
        );

        if (dto.IsConfirmed.HasValue)
        {
            if (dto.IsConfirmed.Value)
                travelHistory.Confirm();
            else
                travelHistory.Unconfirm();
        }

        var updated = await _travelHistoryRepository.UpdateAsync(travelHistory, cancellationToken);
        _logger.LogInformation("✅ 成功更新旅行历史记录: {Id}", updated.Id);

        return MapToDto(updated);
    }

    public async Task<bool> DeleteTravelHistoryAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🗑️ 删除旅行历史记录 - Id: {Id}", id);

        var result = await _travelHistoryRepository.DeleteAsync(id, cancellationToken);
        if (result)
            _logger.LogInformation("✅ 成功删除旅行历史记录: {Id}", id);
        else
            _logger.LogWarning("⚠️ 删除旅行历史记录失败: {Id}", id);

        return result;
    }

    public async Task<bool> ConfirmTravelHistoryAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("✅ 确认旅行历史记录 - Id: {Id}", id);
        return await _travelHistoryRepository.ConfirmAsync(id, cancellationToken);
    }

    public async Task<int> ConfirmBatchTravelHistoryAsync(
        List<string> ids,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("✅ 批量确认旅行历史记录 - Count: {Count}", ids.Count);
        return await _travelHistoryRepository.ConfirmBatchAsync(ids, cancellationToken);
    }

    public async Task<TravelHistoryStats> GetUserTravelStatsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📊 获取用户旅行统计 - UserId: {UserId}", userId);
        return await _travelHistoryRepository.GetUserStatsAsync(userId, cancellationToken);
    }

    #region 私有方法

    /// <summary>
    ///     尝试匹配城市 - 调用 CityService API
    /// </summary>
    private async Task<string?> TryMatchCityAsync(
        string cityName,
        string country,
        string? countryCode,
        double latitude,
        double longitude,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = new CityMatchRequest
            {
                Latitude = latitude,
                Longitude = longitude,
                CityName = cityName,
                CityNameEn = cityName, // 可能是英文名，同时放两个字段
                CountryName = country,
                CountryCode = countryCode
            };

            var result = await _cityServiceClient.MatchCityAsync(request, cancellationToken);
            
            if (result?.IsMatched == true && !string.IsNullOrEmpty(result.CityId))
            {
                _logger.LogInformation(
                    "✅ 城市匹配成功: {CityName} -> CityId={CityId}, Method={Method}",
                    cityName, result.CityId, result.MatchMethod);
                return result.CityId;
            }

            _logger.LogInformation("ℹ️ 未找到匹配的城市: {CityName}, {Country}", cityName, country);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "⚠️ 城市匹配失败: {CityName}", cityName);
            return null;
        }
    }

    private TravelHistoryDto MapToDto(TravelHistory entity)
    {
        List<string>? photos = null;
        if (!string.IsNullOrEmpty(entity.Photos))
        {
            try
            {
                photos = JsonSerializer.Deserialize<List<string>>(entity.Photos);
            }
            catch
            {
                // 忽略解析错误
            }
        }

        return new TravelHistoryDto
        {
            Id = entity.Id,
            UserId = entity.UserId,
            City = entity.City,
            Country = entity.Country,
            CountryCode = entity.CountryCode,
            Latitude = entity.Latitude,
            Longitude = entity.Longitude,
            ArrivalTime = entity.ArrivalTime,
            DepartureTime = entity.DepartureTime,
            IsConfirmed = entity.IsConfirmed,
            Review = entity.Review,
            Rating = entity.Rating,
            Photos = photos,
            CityId = entity.CityId,
            DurationDays = entity.GetDurationDays(),
            IsOngoing = entity.IsOngoing,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    #endregion
}
