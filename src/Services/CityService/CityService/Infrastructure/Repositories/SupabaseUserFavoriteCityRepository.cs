using CityService.Domain.Entities;
using CityService.Domain.Repositories;
using Supabase;
using Postgrest.Models;
using Postgrest.Attributes;
using Microsoft.Extensions.Logging;

namespace CityService.Infrastructure.Repositories;

/// <summary>
/// Supabase用户收藏城市仓储实现
/// </summary>
public class SupabaseUserFavoriteCityRepository : IUserFavoriteCityRepository
{
    private readonly Client _supabaseClient;
    private readonly ILogger<SupabaseUserFavoriteCityRepository> _logger;

    public SupabaseUserFavoriteCityRepository(
        Client supabaseClient,
        ILogger<SupabaseUserFavoriteCityRepository> logger)
    {
        _supabaseClient = supabaseClient;
        _logger = logger;
    }

    public async Task<bool> IsCityFavoritedAsync(Guid userId, string cityId)
    {
        try
        {
            var response = await _supabaseClient
                .From<UserFavoriteCityModel>()
                .Select("id")
                .Filter("user_id", Postgrest.Constants.Operator.Equals, userId.ToString())
                .Filter("city_id", Postgrest.Constants.Operator.Equals, cityId)
                .Single();

            return response != null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "检查收藏状态失败: UserId={UserId}, CityId={CityId}", userId, cityId);
            return false;
        }
    }

    public async Task<UserFavoriteCity> AddFavoriteCityAsync(Guid userId, string cityId)
    {
        try
        {
            // 先检查是否已存在
            var exists = await IsCityFavoritedAsync(userId, cityId);
            if (exists)
            {
                _logger.LogInformation("城市已在收藏列表中: UserId={UserId}, CityId={CityId}", userId, cityId);
                // 返回已存在的记录
                var existing = await _supabaseClient
                    .From<UserFavoriteCityModel>()
                    .Filter("user_id", Postgrest.Constants.Operator.Equals, userId.ToString())
                    .Filter("city_id", Postgrest.Constants.Operator.Equals, cityId)
                    .Single();
                
                if (existing == null)
                {
                    throw new Exception("记录不存在但检查返回已存在");
                }
                
                return MapToEntity(existing);
            }

            var model = new UserFavoriteCityModel
            {
                UserId = userId,
                CityId = cityId
            };

            var response = await _supabaseClient
                .From<UserFavoriteCityModel>()
                .Insert(model);

            var result = response.Models.FirstOrDefault();
            if (result == null)
            {
                throw new Exception("添加收藏失败，返回结果为空");
            }

            _logger.LogInformation("收藏城市成功: UserId={UserId}, CityId={CityId}", userId, cityId);
            return MapToEntity(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加收藏城市失败: UserId={UserId}, CityId={CityId}", userId, cityId);
            throw;
        }
    }

    public async Task<bool> RemoveFavoriteCityAsync(Guid userId, string cityId)
    {
        try
        {
            await _supabaseClient
                .From<UserFavoriteCityModel>()
                .Filter("user_id", Postgrest.Constants.Operator.Equals, userId.ToString())
                .Filter("city_id", Postgrest.Constants.Operator.Equals, cityId)
                .Delete();

            _logger.LogInformation("取消收藏成功: UserId={UserId}, CityId={CityId}", userId, cityId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取消收藏失败: UserId={UserId}, CityId={CityId}", userId, cityId);
            return false;
        }
    }

    public async Task<List<string>> GetUserFavoriteCityIdsAsync(Guid userId)
    {
        try
        {
            var response = await _supabaseClient
                .From<UserFavoriteCityModel>()
                .Select("city_id")
                .Filter("user_id", Postgrest.Constants.Operator.Equals, userId.ToString())
                .Order("created_at", Postgrest.Constants.Ordering.Descending)
                .Get();

            return response.Models.Select(m => m.CityId).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取收藏城市ID列表失败: UserId={UserId}", userId);
            return new List<string>();
        }
    }

    public async Task<(List<UserFavoriteCity> Items, int Total)> GetUserFavoriteCitiesAsync(
        Guid userId, 
        int page = 1, 
        int pageSize = 20)
    {
        try
        {
            var offset = (page - 1) * pageSize;

            var response = await _supabaseClient
                .From<UserFavoriteCityModel>()
                .Filter("user_id", Postgrest.Constants.Operator.Equals, userId.ToString())
                .Order("created_at", Postgrest.Constants.Ordering.Descending)
                .Range(offset, offset + pageSize - 1)
                .Get();

            // 获取总数
            var countResponse = await _supabaseClient
                .From<UserFavoriteCityModel>()
                .Select("id")
                .Filter("user_id", Postgrest.Constants.Operator.Equals, userId.ToString())
                .Get();

            var items = response.Models.Select(MapToEntity).ToList();
            var total = countResponse.Models.Count;

            return (items, total);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取收藏城市列表失败: UserId={UserId}", userId);
            return (new List<UserFavoriteCity>(), 0);
        }
    }

    public async Task<int> GetCityFavoriteCountAsync(string cityId)
    {
        try
        {
            var response = await _supabaseClient
                .From<UserFavoriteCityModel>()
                .Select("id")
                .Filter("city_id", Postgrest.Constants.Operator.Equals, cityId)
                .Get();

            return response.Models.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取城市收藏次数失败: CityId={CityId}", cityId);
            return 0;
        }
    }

    private static UserFavoriteCity MapToEntity(UserFavoriteCityModel model)
    {
        return new UserFavoriteCity
        {
            Id = model.Id,
            UserId = model.UserId,
            CityId = model.CityId,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt
        };
    }
}

/// <summary>
/// Supabase表模型
/// </summary>
[Table("user_favorite_cities")]
public class UserFavoriteCityModel : BaseModel
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("city_id")]
    public string CityId { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
