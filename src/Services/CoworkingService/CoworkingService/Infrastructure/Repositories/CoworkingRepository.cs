using CoworkingService.Domain.Entities;
using CoworkingService.Domain.Repositories;
using Supabase;

namespace CoworkingService.Infrastructure.Repositories;

/// <summary>
/// CoworkingSpace 仓储实现 - Supabase
/// 实现领域层定义的仓储接口
/// </summary>
public class CoworkingRepository : ICoworkingRepository
{
    private readonly Client _supabaseClient;
    private readonly ILogger<CoworkingRepository> _logger;

    public CoworkingRepository(Client supabaseClient, ILogger<CoworkingRepository> logger)
    {
        _supabaseClient = supabaseClient;
        _logger = logger;
    }

    public async Task<CoworkingSpace> CreateAsync(CoworkingSpace coworkingSpace)
    {
        try
        {
            var response = await _supabaseClient
                .From<CoworkingSpace>()
                .Insert(coworkingSpace);

            var created = response.Models.FirstOrDefault();
            if (created == null)
            {
                throw new InvalidOperationException("创建共享办公空间失败");
            }

            _logger.LogInformation("✅ Supabase 创建成功: {Id}", created.Id);
            return created;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Supabase 创建失败");
            throw;
        }
    }

    public async Task<CoworkingSpace?> GetByIdAsync(Guid id)
    {
        try
        {
            var response = await _supabaseClient
                .From<CoworkingSpace>()
                .Where(x => x.Id == id)
                .Single();

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "获取共享办公空间失败: {Id}", id);
            return null;
        }
    }

    public async Task<CoworkingSpace> UpdateAsync(CoworkingSpace coworkingSpace)
    {
        try
        {
            var response = await _supabaseClient
                .From<CoworkingSpace>()
                .Update(coworkingSpace);

            var updated = response.Models.FirstOrDefault();
            if (updated == null)
            {
                throw new InvalidOperationException("更新共享办公空间失败");
            }

            _logger.LogInformation("✅ Supabase 更新成功: {Id}", updated.Id);
            return updated;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Supabase 更新失败: {Id}", coworkingSpace.Id);
            throw;
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        try
        {
            await _supabaseClient
                .From<CoworkingSpace>()
                .Filter("id", Postgrest.Constants.Operator.Equals, id.ToString())
                .Delete();

            _logger.LogInformation("✅ Supabase 删除成功: {Id}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Supabase 删除失败: {Id}", id);
            throw;
        }
    }

    public async Task<(List<CoworkingSpace> Items, int TotalCount)> GetListAsync(
        int page = 1,
        int pageSize = 20,
        Guid? cityId = null,
        bool? isActive = true)
    {
        try
        {
            // 应用分页
            var offset = (page - 1) * pageSize;
            
            List<CoworkingSpace> items;
            
            // 根据不同的过滤条件构建查询
            if (isActive.HasValue && cityId.HasValue)
            {
                var response = await _supabaseClient
                    .From<CoworkingSpace>()
                    .Where(x => x.IsActive == isActive.Value && x.CityId == cityId.Value)
                    .Order(x => x.CreatedAt, Postgrest.Constants.Ordering.Descending)
                    .Range(offset, offset + pageSize - 1)
                    .Get();
                items = response.Models.ToList();
            }
            else if (isActive.HasValue)
            {
                var response = await _supabaseClient
                    .From<CoworkingSpace>()
                    .Where(x => x.IsActive == isActive.Value)
                    .Order(x => x.CreatedAt, Postgrest.Constants.Ordering.Descending)
                    .Range(offset, offset + pageSize - 1)
                    .Get();
                items = response.Models.ToList();
            }
            else if (cityId.HasValue)
            {
                var response = await _supabaseClient
                    .From<CoworkingSpace>()
                    .Where(x => x.CityId == cityId.Value)
                    .Order(x => x.CreatedAt, Postgrest.Constants.Ordering.Descending)
                    .Range(offset, offset + pageSize - 1)
                    .Get();
                items = response.Models.ToList();
            }
            else
            {
                var response = await _supabaseClient
                    .From<CoworkingSpace>()
                    .Order(x => x.CreatedAt, Postgrest.Constants.Ordering.Descending)
                    .Range(offset, offset + pageSize - 1)
                    .Get();
                items = response.Models.ToList();
            }

            // 获取总数（这里简化处理，实际应该单独查询）
            var totalCount = items.Count; // TODO: 实现准确的总数查询

            return (items, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取共享办公空间列表失败");
            throw;
        }
    }

    public async Task<List<CoworkingSpace>> GetByCityIdAsync(Guid cityId)
    {
        try
        {
            var response = await _supabaseClient
                .From<CoworkingSpace>()
                .Where(x => x.CityId == cityId && x.IsActive)
                .Order(x => x.Rating, Postgrest.Constants.Ordering.Descending)
                .Get();

            return response.Models;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 按城市获取共享办公空间失败: {CityId}", cityId);
            throw;
        }
    }

    public async Task<List<CoworkingSpace>> SearchAsync(string searchTerm, int page = 1, int pageSize = 20)
    {
        try
        {
            // 获取所有活跃的共享办公空间
            var response = await _supabaseClient
                .From<CoworkingSpace>()
                .Where(x => x.IsActive)
                .Get();

            var spaces = response.Models.AsEnumerable();

            // 客户端过滤（Supabase 文本搜索限制）
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                spaces = spaces.Where(s =>
                    (s.Name != null && s.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                    (s.Address != null && s.Address.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)));
            }

            // 应用分页
            spaces = spaces
                .Skip((page - 1) * pageSize)
                .Take(pageSize);

            return spaces.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 搜索共享办公空间失败: {SearchTerm}", searchTerm);
            throw;
        }
    }

    public async Task<List<CoworkingSpace>> GetByPriceRangeAsync(
        decimal? minPrice,
        decimal? maxPrice,
        string priceType = "day")
    {
        try
        {
            var query = _supabaseClient
                .From<CoworkingSpace>()
                .Where(x => x.IsActive);

            // 客户端过滤价格范围（Supabase 查询限制）
            var response = await query.Get();
            var spaces = response.Models.AsEnumerable();

            if (minPrice.HasValue && maxPrice.HasValue)
            {
                spaces = priceType switch
                {
                    "day" => spaces.Where(x => x.PricePerDay >= minPrice && x.PricePerDay <= maxPrice),
                    "hour" => spaces.Where(x => x.PricePerHour >= minPrice && x.PricePerHour <= maxPrice),
                    "month" => spaces.Where(x => x.PricePerMonth >= minPrice && x.PricePerMonth <= maxPrice),
                    _ => spaces
                };
            }

            return spaces.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 按价格范围获取共享办公空间失败");
            throw;
        }
    }

    public async Task<List<CoworkingSpace>> GetTopRatedAsync(int limit = 10)
    {
        try
        {
            var response = await _supabaseClient
                .From<CoworkingSpace>()
                .Where(x => x.IsActive)
                .Order(x => x.Rating, Postgrest.Constants.Ordering.Descending)
                .Limit(limit)
                .Get();

            return response.Models;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 获取评分最高的共享办公空间失败");
            throw;
        }
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        try
        {
            var space = await GetByIdAsync(id);
            return space != null;
        }
        catch
        {
            return false;
        }
    }
}
