using UserService.Application.DTOs;
using UserService.Domain.Entities;
using UserService.Domain.Repositories;

namespace UserService.Application.Services;

/// <summary>
///     访问地点服务实现
/// </summary>
public class VisitedPlaceService : IVisitedPlaceService
{
    private readonly ILogger<VisitedPlaceService> _logger;
    private readonly IVisitedPlaceRepository _visitedPlaceRepository;
    private readonly ITravelHistoryRepository _travelHistoryRepository;

    public VisitedPlaceService(
        IVisitedPlaceRepository visitedPlaceRepository,
        ITravelHistoryRepository travelHistoryRepository,
        ILogger<VisitedPlaceService> logger)
    {
        _visitedPlaceRepository = visitedPlaceRepository;
        _travelHistoryRepository = travelHistoryRepository;
        _logger = logger;
    }

    public async Task<List<VisitedPlaceDto>> GetVisitedPlacesByTravelHistoryIdAsync(
        string travelHistoryId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📋 获取旅行访问地点列表 - TravelHistoryId: {TravelHistoryId}", travelHistoryId);

        var places = await _visitedPlaceRepository.GetByTravelHistoryIdAsync(travelHistoryId, cancellationToken);
        return places.Select(MapToDto).ToList();
    }

    public async Task<List<VisitedPlaceDto>> GetHighlightPlacesByTravelHistoryIdAsync(
        string travelHistoryId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📋 获取旅行精选地点列表 - TravelHistoryId: {TravelHistoryId}", travelHistoryId);

        var places = await _visitedPlaceRepository.GetHighlightsByTravelHistoryIdAsync(travelHistoryId, cancellationToken);
        return places.Select(MapToDto).ToList();
    }

    public async Task<(List<VisitedPlaceDto> Items, int Total)> GetUserVisitedPlacesAsync(
        string userId,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📋 获取用户访问地点列表 - UserId: {UserId}, Page: {Page}", userId, page);

        var (places, total) = await _visitedPlaceRepository.GetByUserIdAsync(userId, page, pageSize, cancellationToken);
        return (places.Select(MapToDto).ToList(), total);
    }

    public async Task<VisitedPlaceDto?> GetVisitedPlaceByIdAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🔍 获取访问地点详情 - Id: {Id}", id);

        var place = await _visitedPlaceRepository.GetByIdAsync(id, cancellationToken);
        return place != null ? MapToDto(place) : null;
    }

    public async Task<VisitedPlaceDto> CreateVisitedPlaceAsync(
        string userId,
        CreateVisitedPlaceDto dto,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📝 创建访问地点 - UserId: {UserId}, TravelHistoryId: {TravelHistoryId}, PlaceName: {PlaceName}",
            userId, dto.TravelHistoryId, dto.PlaceName);

        // 验证旅行历史存在且属于该用户
        var travelHistory = await _travelHistoryRepository.GetByIdAsync(dto.TravelHistoryId, cancellationToken);
        if (travelHistory == null)
        {
            throw new InvalidOperationException($"旅行历史记录不存在: {dto.TravelHistoryId}");
        }
        if (travelHistory.UserId != userId)
        {
            throw new UnauthorizedAccessException("无权操作此旅行历史记录");
        }

        // 检查是否已存在（通过 ClientId 去重）
        if (!string.IsNullOrEmpty(dto.ClientId))
        {
            var existing = await _visitedPlaceRepository.GetByClientIdAsync(dto.ClientId, userId, cancellationToken);
            if (existing != null)
            {
                _logger.LogInformation("⚠️ 访问地点已存在，返回现有记录 - ClientId: {ClientId}", dto.ClientId);
                return MapToDto(existing);
            }
        }

        // 检查是否存在相似记录（位置和时间相近）
        var tolerance = TimeSpan.FromMinutes(30);
        var existsSimilar = await _visitedPlaceRepository.ExistsSimilarAsync(
            dto.TravelHistoryId, dto.Latitude, dto.Longitude, dto.ArrivalTime, tolerance, cancellationToken);

        if (existsSimilar)
        {
            _logger.LogWarning("⚠️ 已存在相似的访问地点: Lat={Lat}, Lng={Lng}, Time={Time}",
                dto.Latitude, dto.Longitude, dto.ArrivalTime);
            // 可以选择抛出异常或返回现有记录，这里选择继续创建但记录警告
        }

        var visitedPlace = VisitedPlace.Create(
            dto.TravelHistoryId,
            userId,
            dto.Latitude,
            dto.Longitude,
            dto.ArrivalTime,
            dto.DepartureTime,
            dto.PlaceName,
            dto.PlaceType,
            dto.Address,
            dto.ClientId);

        if (dto.PhotoUrl != null)
            visitedPlace.PhotoUrl = dto.PhotoUrl;
        if (dto.Notes != null)
            visitedPlace.Notes = dto.Notes;
        if (dto.IsHighlight)
            visitedPlace.IsHighlight = true;
        if (dto.GooglePlaceId != null)
            visitedPlace.GooglePlaceId = dto.GooglePlaceId;

        var created = await _visitedPlaceRepository.CreateAsync(visitedPlace, cancellationToken);
        _logger.LogInformation("✅ 成功创建访问地点 - Id: {Id}", created.Id);

        return MapToDto(created);
    }

    public async Task<List<VisitedPlaceDto>> CreateBatchVisitedPlacesAsync(
        string userId,
        BatchCreateVisitedPlaceDto dto,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📝 批量创建访问地点 - UserId: {UserId}, TravelHistoryId: {TravelHistoryId}, Count: {Count}",
            userId, dto.TravelHistoryId, dto.Items.Count);

        // 验证旅行历史存在且属于该用户
        var travelHistory = await _travelHistoryRepository.GetByIdAsync(dto.TravelHistoryId, cancellationToken);
        if (travelHistory == null)
        {
            throw new InvalidOperationException($"旅行历史记录不存在: {dto.TravelHistoryId}");
        }
        if (travelHistory.UserId != userId)
        {
            throw new UnauthorizedAccessException("无权操作此旅行历史记录");
        }

        var results = new List<VisitedPlaceDto>();
        var newPlaces = new List<VisitedPlace>();

        foreach (var item in dto.Items)
        {
            // 设置 TravelHistoryId
            item.TravelHistoryId = dto.TravelHistoryId;

            // 检查是否已存在（通过 ClientId 去重）
            if (!string.IsNullOrEmpty(item.ClientId))
            {
                var existing = await _visitedPlaceRepository.GetByClientIdAsync(item.ClientId, userId, cancellationToken);
                if (existing != null)
                {
                    _logger.LogInformation("⏭️ 跳过已存在的访问地点 - ClientId: {ClientId}", item.ClientId);
                    results.Add(MapToDto(existing));
                    continue;
                }
            }

            var visitedPlace = VisitedPlace.Create(
                dto.TravelHistoryId,
                userId,
                item.Latitude,
                item.Longitude,
                item.ArrivalTime,
                item.DepartureTime,
                item.PlaceName,
                item.PlaceType,
                item.Address,
                item.ClientId);

            if (item.PhotoUrl != null)
                visitedPlace.PhotoUrl = item.PhotoUrl;
            if (item.Notes != null)
                visitedPlace.Notes = item.Notes;
            if (item.IsHighlight)
                visitedPlace.IsHighlight = true;
            if (item.GooglePlaceId != null)
                visitedPlace.GooglePlaceId = item.GooglePlaceId;

            newPlaces.Add(visitedPlace);
        }

        if (newPlaces.Any())
        {
            var created = await _visitedPlaceRepository.CreateBatchAsync(newPlaces, cancellationToken);
            results.AddRange(created.Select(MapToDto));
            _logger.LogInformation("✅ 成功批量创建 {Count} 个访问地点", created.Count);
        }

        return results;
    }

    public async Task<VisitedPlaceDto?> UpdateVisitedPlaceAsync(
        string id,
        string userId,
        UpdateVisitedPlaceDto dto,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📝 更新访问地点 - Id: {Id}, UserId: {UserId}", id, userId);

        var place = await _visitedPlaceRepository.GetByIdAsync(id, cancellationToken);
        if (place == null)
        {
            _logger.LogWarning("⚠️ 访问地点不存在 - Id: {Id}", id);
            return null;
        }

        if (place.UserId != userId)
        {
            throw new UnauthorizedAccessException("无权操作此访问地点");
        }

        // 更新字段
        if (dto.PlaceName != null)
            place.PlaceName = dto.PlaceName;
        if (dto.PlaceType != null)
            place.PlaceType = dto.PlaceType;
        if (dto.Address != null)
            place.Address = dto.Address;
        if (dto.PhotoUrl != null)
            place.PhotoUrl = dto.PhotoUrl;
        if (dto.Notes != null)
            place.Notes = dto.Notes;
        if (dto.IsHighlight.HasValue)
            place.IsHighlight = dto.IsHighlight.Value;
        if (dto.GooglePlaceId != null)
            place.GooglePlaceId = dto.GooglePlaceId;

        place.UpdatedAt = DateTime.UtcNow;

        var updated = await _visitedPlaceRepository.UpdateAsync(place, cancellationToken);
        _logger.LogInformation("✅ 成功更新访问地点 - Id: {Id}", updated.Id);

        return MapToDto(updated);
    }

    public async Task<bool> DeleteVisitedPlaceAsync(
        string id,
        string userId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("🗑️ 删除访问地点 - Id: {Id}, UserId: {UserId}", id, userId);

        var place = await _visitedPlaceRepository.GetByIdAsync(id, cancellationToken);
        if (place == null)
        {
            _logger.LogWarning("⚠️ 访问地点不存在 - Id: {Id}", id);
            return false;
        }

        if (place.UserId != userId)
        {
            throw new UnauthorizedAccessException("无权操作此访问地点");
        }

        var result = await _visitedPlaceRepository.DeleteAsync(id, cancellationToken);
        if (result)
        {
            _logger.LogInformation("✅ 成功删除访问地点 - Id: {Id}", id);
        }

        return result;
    }

    public async Task<VisitedPlaceDto?> ToggleHighlightAsync(
        string id,
        string userId,
        bool isHighlight,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("⭐ 切换精选状态 - Id: {Id}, UserId: {UserId}, IsHighlight: {IsHighlight}",
            id, userId, isHighlight);

        var place = await _visitedPlaceRepository.GetByIdAsync(id, cancellationToken);
        if (place == null)
        {
            _logger.LogWarning("⚠️ 访问地点不存在 - Id: {Id}", id);
            return null;
        }

        if (place.UserId != userId)
        {
            throw new UnauthorizedAccessException("无权操作此访问地点");
        }

        if (isHighlight)
            place.MarkAsHighlight();
        else
            place.UnmarkAsHighlight();

        var updated = await _visitedPlaceRepository.UpdateAsync(place, cancellationToken);
        _logger.LogInformation("✅ 成功切换精选状态 - Id: {Id}, IsHighlight: {IsHighlight}", updated.Id, updated.IsHighlight);

        return MapToDto(updated);
    }

    public async Task<TravelVisitedPlaceStatsDto> GetVisitedPlaceStatsAsync(
        string travelHistoryId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("📊 获取旅行访问地点统计 - TravelHistoryId: {TravelHistoryId}", travelHistoryId);

        var stats = await _visitedPlaceRepository.GetStatsByTravelHistoryIdAsync(travelHistoryId, cancellationToken);

        return new TravelVisitedPlaceStatsDto
        {
            TravelHistoryId = travelHistoryId,
            TotalPlaces = stats.TotalPlaces,
            HighlightPlaces = stats.HighlightPlaces,
            TotalDurationMinutes = stats.TotalDurationMinutes,
            PlaceTypeDistribution = stats.PlaceTypeDistribution
        };
    }

    #region 私有方法

    private static VisitedPlaceDto MapToDto(VisitedPlace place)
    {
        return new VisitedPlaceDto
        {
            Id = place.Id,
            TravelHistoryId = place.TravelHistoryId,
            UserId = place.UserId,
            Latitude = place.Latitude,
            Longitude = place.Longitude,
            PlaceName = place.PlaceName,
            PlaceType = place.PlaceType,
            Address = place.Address,
            ArrivalTime = place.ArrivalTime,
            DepartureTime = place.DepartureTime,
            DurationMinutes = place.DurationMinutes,
            PhotoUrl = place.PhotoUrl,
            Notes = place.Notes,
            IsHighlight = place.IsHighlight,
            GooglePlaceId = place.GooglePlaceId,
            ClientId = place.ClientId,
            FormattedDuration = place.FormattedDuration,
            IconType = place.IconType,
            CreatedAt = place.CreatedAt,
            UpdatedAt = place.UpdatedAt
        };
    }

    #endregion
}
