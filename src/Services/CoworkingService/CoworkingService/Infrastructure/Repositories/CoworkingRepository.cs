using CoworkingService.Domain.Entities;
using CoworkingService.Domain.Repositories;
using Postgrest;
using Client = Supabase.Client;

namespace CoworkingService.Infrastructure.Repositories;

/// <summary>
///     CoworkingSpace 仓储实现 - Supabase
///     实现领域层定义的仓储接口
/// </summary>
public class CoworkingRepository : ICoworkingRepository
{
    private readonly ILogger<CoworkingRepository> _logger;
    private readonly Client _supabaseClient;

    public CoworkingRepository(Client supabaseClient, ILogger<CoworkingRepository> logger)
    {
        _supabaseClient = supabaseClient;
        _logger = logger;
    }

    public async Task<CoworkingSpace> CreateAsync(CoworkingSpace coworkingSpace)
    {
        try
        {
            _logger.LogInformation("📝 准备创建 Coworking: Name={Name}, Address={Address}",
                coworkingSpace.Name, coworkingSpace.Address);

            // 使用 Upsert 替代 Insert 以获取完整返回
            var response = await _supabaseClient
                .From<CoworkingSpace>()
                .Upsert(coworkingSpace);

            _logger.LogInformation("📊 Upsert 响应: ModelCount={Count}", response.Models?.Count ?? 0);

            var created = response.Models?.FirstOrDefault();
            if (created == null)
            {
                _logger.LogError("❌ Upsert 未返回任何数据");
                throw new InvalidOperationException("创建共享办公空间失败：无返回数据");
            }

            _logger.LogInformation("🔍 返回的数据: Id={Id}, Name={Name}, Address={Address}",
                created.Id, created.Name ?? "null", created.Address ?? "null");

            // 如果返回的数据不完整，尝试重新查询
            if (created.Id == Guid.Empty || string.IsNullOrEmpty(created.Name))
            {
                _logger.LogWarning("⚠️  返回数据不完整，尝试按时间戳查询最新记录");

                // 按创建时间倒序查询，获取最新的一条
                var queryResponse = await _supabaseClient
                    .From<CoworkingSpace>()
                    .Order("created_at", Constants.Ordering.Descending)
                    .Limit(1)
                    .Get();

                _logger.LogInformation("📊 查询响应: ModelCount={Count}", queryResponse.Models?.Count ?? 0);

                created = queryResponse.Models?.FirstOrDefault();
                if (created == null) throw new InvalidOperationException("创建后无法查询到记录");

                _logger.LogInformation("🔍 查询到的数据: Id={Id}, Name={Name}", created.Id, created.Name);
            }

            _logger.LogInformation("✅ Supabase 创建成功: {Id}, Name: {Name}", created.Id, created.Name);
            return created;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Supabase 创建失败: {Message}", ex.Message);
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
                .Filter("is_deleted", Constants.Operator.NotEqual, "true")
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
            if (updated == null) throw new InvalidOperationException("更新共享办公空间失败");

            _logger.LogInformation("✅ Supabase 更新成功: {Id}", updated.Id);
            return updated;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Supabase 更新失败: {Id}", coworkingSpace.Id);
            throw;
        }
    }

    public async Task DeleteAsync(Guid id, Guid? deletedBy = null)
    {
        try
        {
            // 逻辑删除：设置 IsDeleted = true
            var coworkingSpace = await GetByIdAsync(id);
            if (coworkingSpace == null)
            {
                _logger.LogWarning("⚠️ 要删除的共享办公空间不存在: {Id}", id);
                return;
            }

            coworkingSpace.MarkAsDeleted(deletedBy);

            await _supabaseClient
                .From<CoworkingSpace>()
                .Where(x => x.Id == id)
                .Set(x => x.IsDeleted, true)
                .Set(x => x.DeletedAt!, coworkingSpace.DeletedAt)
                .Set(x => x.DeletedBy!, deletedBy)
                .Set(x => x.UpdatedAt, coworkingSpace.UpdatedAt)
                .Set(x => x.UpdatedBy!, deletedBy)
                .Update();

            _logger.LogInformation("✅ Supabase 逻辑删除成功: {Id}, DeletedBy: {DeletedBy}", id, deletedBy);
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
                    .Filter("is_deleted", Constants.Operator.NotEqual, "true")
                    .Order(x => x.CreatedAt, Constants.Ordering.Descending)
                    .Range(offset, offset + pageSize - 1)
                    .Get();
                items = response.Models.ToList();
            }
            else if (isActive.HasValue)
            {
                var response = await _supabaseClient
                    .From<CoworkingSpace>()
                    .Where(x => x.IsActive == isActive.Value)
                    .Filter("is_deleted", Constants.Operator.NotEqual, "true")
                    .Order(x => x.CreatedAt, Constants.Ordering.Descending)
                    .Range(offset, offset + pageSize - 1)
                    .Get();
                items = response.Models.ToList();
            }
            else if (cityId.HasValue)
            {
                var response = await _supabaseClient
                    .From<CoworkingSpace>()
                    .Where(x => x.CityId == cityId.Value)
                    .Filter("is_deleted", Constants.Operator.NotEqual, "true")
                    .Order(x => x.CreatedAt, Constants.Ordering.Descending)
                    .Range(offset, offset + pageSize - 1)
                    .Get();
                items = response.Models.ToList();
            }
            else
            {
                var response = await _supabaseClient
                    .From<CoworkingSpace>()
                    .Filter("is_deleted", Constants.Operator.NotEqual, "true")
                    .Order(x => x.CreatedAt, Constants.Ordering.Descending)
                    .Range(offset, offset + pageSize - 1)
                    .Get();
                items = response.Models.ToList();
            }

            // 获取准确的总数
            int totalCount;
            try
            {
                if (isActive.HasValue && cityId.HasValue)
                {
                    var countResponse = await _supabaseClient
                        .From<CoworkingSpace>()
                        .Where(x => x.IsActive == isActive.Value && x.CityId == cityId.Value)
                        .Filter("is_deleted", Constants.Operator.NotEqual, "true")
                        .Count(Constants.CountType.Exact);
                    totalCount = countResponse;
                }
                else if (isActive.HasValue)
                {
                    var countResponse = await _supabaseClient
                        .From<CoworkingSpace>()
                        .Where(x => x.IsActive == isActive.Value)
                        .Filter("is_deleted", Constants.Operator.NotEqual, "true")
                        .Count(Constants.CountType.Exact);
                    totalCount = countResponse;
                }
                else if (cityId.HasValue)
                {
                    var countResponse = await _supabaseClient
                        .From<CoworkingSpace>()
                        .Where(x => x.CityId == cityId.Value)
                        .Filter("is_deleted", Constants.Operator.NotEqual, "true")
                        .Count(Constants.CountType.Exact);
                    totalCount = countResponse;
                }
                else
                {
                    var countResponse = await _supabaseClient
                        .From<CoworkingSpace>()
                        .Filter("is_deleted", Constants.Operator.NotEqual, "true")
                        .Count(Constants.CountType.Exact);
                    totalCount = countResponse;
                }
            }
            catch (Exception countEx)
            {
                _logger.LogWarning(countEx, "⚠️ 获取总数失败，使用当前页数量作为估计");
                totalCount = items.Count;
            }

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
                .Filter("city_id", Constants.Operator.Equals, cityId.ToString())
                .Filter("is_active", Constants.Operator.Equals, "true")
                .Filter("is_deleted", Constants.Operator.NotEqual, "true")
                .Order(x => x.Rating, Constants.Ordering.Descending)
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
            var offset = (page - 1) * pageSize;

            // 使用数据库级 ILIKE 过滤，避免加载所有数据到内存
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                // PostgREST 使用 or 条件进行多字段 ILIKE 搜索
                var searchPattern = $"%{searchTerm}%";
                var response = await _supabaseClient
                    .From<CoworkingSpace>()
                    .Where(x => x.IsActive)
                    .Filter("is_deleted", Constants.Operator.NotEqual, "true")
                    .Filter("or", Constants.Operator.Equals, $"(name.ilike.{searchPattern},address.ilike.{searchPattern})")
                    .Order(x => x.CreatedAt, Constants.Ordering.Descending)
                    .Range(offset, offset + pageSize - 1)
                    .Get();

                _logger.LogInformation("✅ 搜索共享办公空间: Term={SearchTerm}, Found={Count}",
                    searchTerm, response.Models.Count);
                return response.Models.ToList();
            }
            else
            {
                // 无搜索词时，直接分页获取
                var response = await _supabaseClient
                    .From<CoworkingSpace>()
                    .Where(x => x.IsActive)
                    .Filter("is_deleted", Constants.Operator.NotEqual, "true")
                    .Order(x => x.CreatedAt, Constants.Ordering.Descending)
                    .Range(offset, offset + pageSize - 1)
                    .Get();

                return response.Models.ToList();
            }
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
            // 客户端过滤价格范围（Supabase 查询限制）
            var response = await _supabaseClient
                .From<CoworkingSpace>()
                .Where(x => x.IsActive)
                .Filter("is_deleted", Constants.Operator.NotEqual, "true")
                .Get();
            var spaces = response.Models.AsEnumerable();

            if (minPrice.HasValue && maxPrice.HasValue)
                spaces = priceType switch
                {
                    "day" => spaces.Where(x => x.PricePerDay >= minPrice && x.PricePerDay <= maxPrice),
                    "hour" => spaces.Where(x => x.PricePerHour >= minPrice && x.PricePerHour <= maxPrice),
                    "month" => spaces.Where(x => x.PricePerMonth >= minPrice && x.PricePerMonth <= maxPrice),
                    _ => spaces
                };

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
                .Filter("is_deleted", Constants.Operator.NotEqual, "true")
                .Order(x => x.Rating, Constants.Ordering.Descending)
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

    #region 冗余字段更新方法

    public async Task<int> UpdateCreatorInfoAsync(Guid creatorId, string? creatorName, string? creatorAvatar)
    {
        try
        {
            _logger.LogInformation("🔄 更新创建者信息: CreatorId={CreatorId}, Name={Name}", creatorId, creatorName);

            // 获取该创建者的所有 Coworking 空间
            var response = await _supabaseClient
                .From<CoworkingSpace>()
                .Filter("created_by", Constants.Operator.Equals, creatorId.ToString())
                .Filter("is_deleted", Constants.Operator.NotEqual, "true")
                .Get();

            var spaces = response.Models;
            if (!spaces.Any())
            {
                _logger.LogInformation("ℹ️ 该创建者没有 Coworking 空间需要更新");
                return 0;
            }

            // 批量更新
            var updatedCount = 0;
            foreach (var space in spaces)
            {
                space.CreatorName = creatorName;
                space.CreatorAvatar = creatorAvatar;
                space.UpdatedAt = DateTime.UtcNow;

                await _supabaseClient
                    .From<CoworkingSpace>()
                    .Upsert(space);
                updatedCount++;
            }

            _logger.LogInformation("✅ 已更新 {Count} 个 Coworking 空间的创建者信息", updatedCount);
            return updatedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 更新创建者信息失败: CreatorId={CreatorId}", creatorId);
            throw;
        }
    }

    public async Task<int> UpdateCityInfoAsync(Guid cityId, string? cityName, string? cityNameEn, string? cityCountry)
    {
        try
        {
            _logger.LogInformation("🔄 更新城市信息: CityId={CityId}, Name={Name}, Country={Country}",
                cityId, cityName, cityCountry);

            // 获取该城市的所有 Coworking 空间
            var response = await _supabaseClient
                .From<CoworkingSpace>()
                .Filter("city_id", Constants.Operator.Equals, cityId.ToString())
                .Filter("is_deleted", Constants.Operator.NotEqual, "true")
                .Get();

            var spaces = response.Models;
            if (!spaces.Any())
            {
                _logger.LogInformation("ℹ️ 该城市没有 Coworking 空间需要更新");
                return 0;
            }

            // 批量更新
            var updatedCount = 0;
            foreach (var space in spaces)
            {
                space.CityName = cityName;
                space.CityNameEn = cityNameEn;
                space.CityCountry = cityCountry;
                space.UpdatedAt = DateTime.UtcNow;

                await _supabaseClient
                    .From<CoworkingSpace>()
                    .Upsert(space);
                updatedCount++;
            }

            _logger.LogInformation("✅ 已更新 {Count} 个 Coworking 空间的城市信息", updatedCount);
            return updatedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 更新城市信息失败: CityId={CityId}", cityId);
            throw;
        }
    }

    public Task FillRedundantFieldsAsync(CoworkingSpace coworkingSpace, string? creatorName, string? creatorAvatar,
        string? cityName, string? cityNameEn, string? cityCountry)
    {
        coworkingSpace.CreatorName = creatorName;
        coworkingSpace.CreatorAvatar = creatorAvatar;
        coworkingSpace.CityName = cityName;
        coworkingSpace.CityNameEn = cityNameEn;
        coworkingSpace.CityCountry = cityCountry;
        return Task.CompletedTask;
    }

    /// <summary>
    ///     批量获取城市的 Coworking 空间数量（优化版：单次查询）
    /// </summary>
    public async Task<Dictionary<Guid, int>> GetCoworkingCountsByCityIdsAsync(List<Guid> cityIds)
    {
        var result = new Dictionary<Guid, int>();

        if (cityIds.Count == 0)
            return result;

        try
        {
            _logger.LogInformation("📊 [优化] 批量获取 {Count} 个城市的 Coworking 数量 (单次查询)", cityIds.Count);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // 构建 IN 查询 - 一次性获取所有指定城市的 Coworking 空间
            var cityIdStrings = cityIds.Select(id => id.ToString()).ToList();

            var response = await _supabaseClient
                .From<CoworkingSpace>()
                .Select("id, city_id")
                .Filter("is_deleted", Constants.Operator.NotEqual, "true")
                .Filter("is_active", Constants.Operator.Equals, "true")
                .Filter("city_id", Constants.Operator.In, cityIdStrings)
                .Get();

            var spaces = response.Models.ToList();

            // 按城市ID分组计数（过滤掉 CityId 为 null 的记录）
            var groupedCounts = spaces
                .Where(s => s.CityId.HasValue)
                .GroupBy(s => s.CityId!.Value)
                .ToDictionary(g => g.Key, g => g.Count());

            // 确保所有请求的城市都有结果（没有 Coworking 的城市计数为0）
            foreach (var cityId in cityIds)
            {
                result[cityId] = groupedCounts.GetValueOrDefault(cityId, 0);
            }

            stopwatch.Stop();
            _logger.LogInformation("✅ [优化] 批量获取城市 Coworking 数量完成: {Count} 个城市, 耗时 {Elapsed}ms",
                result.Count, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 批量获取城市 Coworking 数量失败");
            return result;
        }
    }

    #endregion
}
