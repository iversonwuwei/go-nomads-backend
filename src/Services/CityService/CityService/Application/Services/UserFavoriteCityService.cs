using CityService.Domain.Entities;
using CityService.Domain.Repositories;

namespace CityService.Application.Services;

/// <summary>
///     用户收藏城市应用服务接口
/// </summary>
public interface IUserFavoriteCityService
{
    Task<bool> IsCityFavoritedAsync(Guid userId, string cityId);
    Task<UserFavoriteCity> AddFavoriteCityAsync(Guid userId, string cityId);
    Task<bool> RemoveFavoriteCityAsync(Guid userId, string cityId);
    Task<List<string>> GetUserFavoriteCityIdsAsync(Guid userId);
    Task<(List<UserFavoriteCity> Items, int Total)> GetUserFavoriteCitiesAsync(Guid userId, int page, int pageSize);
    Task<int> GetUserFavoriteCitiesCountAsync(Guid userId);
}

/// <summary>
///     用户收藏城市应用服务实现
/// </summary>
public class UserFavoriteCityService : IUserFavoriteCityService
{
    private readonly ILogger<UserFavoriteCityService> _logger;
    private readonly IUserFavoriteCityRepository _repository;

    public UserFavoriteCityService(
        IUserFavoriteCityRepository repository,
        ILogger<UserFavoriteCityService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<bool> IsCityFavoritedAsync(Guid userId, string cityId)
    {
        if (string.IsNullOrWhiteSpace(cityId)) throw new ArgumentException("城市ID不能为空", nameof(cityId));

        return await _repository.IsCityFavoritedAsync(userId, cityId);
    }

    public async Task<UserFavoriteCity> AddFavoriteCityAsync(Guid userId, string cityId)
    {
        if (string.IsNullOrWhiteSpace(cityId)) throw new ArgumentException("城市ID不能为空", nameof(cityId));

        _logger.LogInformation("用户添加收藏城市: UserId={UserId}, CityId={CityId}", userId, cityId);
        return await _repository.AddFavoriteCityAsync(userId, cityId);
    }

    public async Task<bool> RemoveFavoriteCityAsync(Guid userId, string cityId)
    {
        if (string.IsNullOrWhiteSpace(cityId)) throw new ArgumentException("城市ID不能为空", nameof(cityId));

        _logger.LogInformation("用户取消收藏城市: UserId={UserId}, CityId={CityId}", userId, cityId);
        return await _repository.RemoveFavoriteCityAsync(userId, cityId);
    }

    public async Task<List<string>> GetUserFavoriteCityIdsAsync(Guid userId)
    {
        return await _repository.GetUserFavoriteCityIdsAsync(userId);
    }

    public async Task<(List<UserFavoriteCity> Items, int Total)> GetUserFavoriteCitiesAsync(
        Guid userId,
        int page,
        int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        return await _repository.GetUserFavoriteCitiesAsync(userId, page, pageSize);
    }

    public async Task<int> GetUserFavoriteCitiesCountAsync(Guid userId)
    {
        return await _repository.GetUserFavoriteCitiesCountAsync(userId);
    }
}