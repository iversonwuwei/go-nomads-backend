using CityService.Domain.Entities;
using CityService.Domain.Repositories;
using CityService.Domain.ValueObjects;
using Postgrest;
using Postgrest.Interfaces;
using Shared.Repositories;
using Client = Supabase.Client;

namespace CityService.Infrastructure.Repositories;

/// <summary>
///     基于 Supabase 的城市仓储实现
/// </summary>
public partial class SupabaseCityRepository : SupabaseRepositoryBase<City>, ICityRepository
{
    private readonly IConfiguration _configuration;

    public SupabaseCityRepository(Client supabaseClient, ILogger<SupabaseCityRepository> logger, IConfiguration configuration)
        : base(supabaseClient, logger)
    {
        _configuration = configuration;
    }

    public async Task<IEnumerable<City>> GetAllAsync(int pageNumber, int pageSize)
    {
        var offset = (pageNumber - 1) * pageSize;

        var response = await SupabaseClient
            .From<City>()
            .Filter("is_active", Constants.Operator.Equals, "true")
            .Filter("is_deleted", Constants.Operator.NotEqual, "true")
            .Order(x => x.OverallScore!, Constants.Ordering.Descending)
            .Range(offset, offset + pageSize - 1)
            .Get();

        return response.Models;
    }

    public async Task<City?> GetByIdAsync(Guid id)
    {
        try
        {
            var response = await SupabaseClient
                .From<City>()
                .Where(x => x.Id == id)
                .Filter("is_deleted", Constants.Operator.NotEqual, "true")
                .Single();

            if (response != null)
            {
                // Postgrest ORM 对 List<string> 数组类型支持不完善，需要手动获取
                await EnrichCityWithImageUrlsAsync(response);
                
                // 调试日志
                Logger.LogInformation(
                    "🔍 [GetByIdAsync] 从数据库读取城市: Id={Id}, Name={Name}, LandscapeImageUrls={LandscapeImageUrls}, Count={Count}",
                    response.Id, response.Name, 
                    response.LandscapeImageUrls != null ? string.Join(", ", response.LandscapeImageUrls) : "null",
                    response.LandscapeImageUrls?.Count ?? 0);
            }

            return response;
        }
        catch
        {
            return null;
        }
    }
    
    /// <summary>
    /// 手动获取城市的图片 URL 字段（绕过 Postgrest ORM 的数组解析问题）
    /// </summary>
    private async Task EnrichCityWithImageUrlsAsync(City city)
    {
        try
        {
            var supabaseUrl = _configuration["Supabase:Url"];
            var supabaseKey = _configuration["Supabase:Key"];

            if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(supabaseKey))
            {
                return;
            }

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("apikey", supabaseKey);
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {supabaseKey}");

            var requestUrl = $"{supabaseUrl}/rest/v1/cities?id=eq.{city.Id}&select=landscape_image_urls";
            var response = await httpClient.GetAsync(requestUrl);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var jsonDoc = System.Text.Json.JsonDocument.Parse(content);
                var root = jsonDoc.RootElement;
                
                if (root.ValueKind == System.Text.Json.JsonValueKind.Array && root.GetArrayLength() > 0)
                {
                    var firstItem = root[0];
                    if (firstItem.TryGetProperty("landscape_image_urls", out var landscapeUrls) && 
                        landscapeUrls.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        var urls = new List<string>();
                        foreach (var url in landscapeUrls.EnumerateArray())
                        {
                            if (url.ValueKind == System.Text.Json.JsonValueKind.String)
                            {
                                urls.Add(url.GetString()!);
                            }
                        }
                        city.LandscapeImageUrls = urls;
                        Logger.LogDebug("✅ [EnrichCityWithImageUrlsAsync] 成功获取 {Count} 张横屏图片", urls.Count);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "⚠️ [EnrichCityWithImageUrlsAsync] 获取图片 URL 失败");
        }
    }

    public async Task<IEnumerable<City>> SearchAsync(CitySearchCriteria criteria)
    {
        var offset = (criteria.PageNumber - 1) * criteria.PageSize;

        // 🚀 优化：在数据库级别进行过滤，而不是加载所有数据到内存
        var query = SupabaseClient
            .From<City>()
            .Filter("is_active", Constants.Operator.Equals, "true")
            .Filter("is_deleted", Constants.Operator.NotEqual, "true");

        // 使用数据库级别的 ILIKE 搜索（支持中英文）
        if (!string.IsNullOrWhiteSpace(criteria.Name))
        {
            // 使用 Or() 方法进行多列模糊匹配
            var searchPattern = $"%{criteria.Name}%";
            var filters = new List<IPostgrestQueryFilter>
            {
                new QueryFilter("name", Constants.Operator.ILike, searchPattern),
                new QueryFilter("name_en", Constants.Operator.ILike, searchPattern)
            };
            query = query.Or(filters);
        }

        // 国家过滤
        if (!string.IsNullOrWhiteSpace(criteria.Country))
        {
            query = query.Filter("country", Constants.Operator.ILike, $"%{criteria.Country}%");
        }

        // 地区过滤
        if (!string.IsNullOrWhiteSpace(criteria.Region))
        {
            query = query.Filter("region", Constants.Operator.ILike, $"%{criteria.Region}%");
        }

        // 费用过滤
        if (criteria.MinCostOfLiving.HasValue)
        {
            query = query.Filter("average_cost_of_living", Constants.Operator.GreaterThanOrEqual,
                criteria.MinCostOfLiving.Value.ToString());
        }

        if (criteria.MaxCostOfLiving.HasValue)
        {
            query = query.Filter("average_cost_of_living", Constants.Operator.LessThanOrEqual,
                criteria.MaxCostOfLiving.Value.ToString());
        }

        // 评分过滤
        if (criteria.MinScore.HasValue)
        {
            query = query.Filter("overall_score", Constants.Operator.GreaterThanOrEqual,
                criteria.MinScore.Value.ToString());
        }

        // 排序和分页（在数据库级别）
        var response = await query
            .Order(x => x.OverallScore!, Constants.Ordering.Descending)
            .Range(offset, offset + criteria.PageSize - 1)
            .Get();

        var cities = response.Models.AsEnumerable();

        // 标签过滤仍需在内存中进行（因为是数组字段）
        if (criteria.Tags is { Count: > 0 })
        {
            cities = cities.Where(c => c.Tags != null && criteria.Tags.All(tag => c.Tags.Contains(tag)));
        }

        Logger.LogInformation("🔍 [SearchAsync] 搜索完成: 条件={Criteria}, 结果数={Count}",
            criteria.Name, cities.Count());

        return cities.ToList();
    }

    public async Task<City> CreateAsync(City city)
    {
        city.Id = Guid.NewGuid();
        city.CreatedAt = DateTime.UtcNow;
        city.UpdatedAt = DateTime.UtcNow;

        var response = await SupabaseClient
            .From<City>()
            .Insert(city);

        return response.Models.First();
    }

    public async Task<City?> UpdateAsync(Guid id, City city)
    {
        city.UpdatedAt = DateTime.UtcNow;

        try
        {
            Logger.LogInformation(
                "🔄 [SupabaseCityRepository] 开始更新城市: Id={Id}, Lat={Lat}, Lng={Lng}, ImageUrl={ImageUrl}, PortraitImageUrl={PortraitImageUrl}, LandscapeCount={LandscapeCount}",
                id, city.Latitude, city.Longitude, city.ImageUrl, city.PortraitImageUrl, city.LandscapeImageUrls?.Count ?? 0);

            // 创建一个用于更新的简化对象，避免 Reference 属性导致的外键关系问题
            var updatePayload = new CityUpdatePayload
            {
                Id = id,
                Name = city.Name,
                NameEn = city.NameEn,
                Country = city.Country,
                Region = city.Region,
                Description = city.Description,
                Latitude = city.Latitude,
                Longitude = city.Longitude,
                Population = city.Population,
                Climate = city.Climate,
                TimeZone = city.TimeZone,
                Currency = city.Currency,
                ImageUrl = city.ImageUrl,
                PortraitImageUrl = city.PortraitImageUrl,
                LandscapeImageUrls = city.LandscapeImageUrls,
                OverallScore = city.OverallScore,
                InternetQualityScore = city.InternetQualityScore,
                SafetyScore = city.SafetyScore,
                CostScore = city.CostScore,
                CommunityScore = city.CommunityScore,
                WeatherScore = city.WeatherScore,
                Tags = city.Tags,
                IsActive = city.IsActive,
                UpdatedAt = city.UpdatedAt
            };

            // 使用简化的更新对象
            var response = await SupabaseClient
                .From<CityUpdatePayload>()
                .Where(x => x.Id == id)
                .Update(updatePayload);

            // 重新获取更新后的城市
            var updatedCity = await GetByIdAsync(id);
            
            if (updatedCity != null)
            {
                Logger.LogInformation(
                    "✅ [SupabaseCityRepository] 城市更新成功: Id={Id}, Lat={Lat}, Lng={Lng}, ImageUrl={ImageUrl}, PortraitImageUrl={PortraitImageUrl}",
                    updatedCity.Id, updatedCity.Latitude, updatedCity.Longitude, updatedCity.ImageUrl, updatedCity.PortraitImageUrl);
            }
            else
            {
                Logger.LogWarning("⚠️ [SupabaseCityRepository] 更新返回空结果: Id={Id}", id);
            }
            
            return updatedCity;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ [SupabaseCityRepository] 更新城市失败: Id={Id}, Error={Error}", id, ex.Message);
            return null;
        }
    }

    /// <summary>
    /// 直接使用 HttpClient 更新城市图片字段，绕过 Postgrest ORM
    /// </summary>
    public async Task<bool> UpdateImagesDirectAsync(Guid cityId, string? imageUrl, string? portraitImageUrl, List<string>? landscapeImageUrls)
    {
        try
        {
            var supabaseUrl = _configuration["Supabase:Url"];
            var supabaseKey = _configuration["Supabase:Key"];

            if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(supabaseKey))
            {
                Logger.LogError("❌ Supabase URL 或 Key 未配置");
                return false;
            }

            Logger.LogInformation(
                "🔄 [UpdateImagesDirectAsync] 开始直接更新图片: CityId={CityId}, ImageUrl={ImageUrl}, PortraitUrl={PortraitUrl}, LandscapeCount={LandscapeCount}",
                cityId, imageUrl, portraitImageUrl, landscapeImageUrls?.Count ?? 0);

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("apikey", supabaseKey);
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {supabaseKey}");
            httpClient.DefaultRequestHeaders.Add("Prefer", "return=representation");

            // 构建更新的 JSON 数据
            var updateData = new Dictionary<string, object?>();
            
            if (!string.IsNullOrEmpty(imageUrl))
            {
                updateData["image_url"] = imageUrl;
            }
            
            if (!string.IsNullOrEmpty(portraitImageUrl))
            {
                updateData["portrait_image_url"] = portraitImageUrl;
            }
            
            if (landscapeImageUrls != null && landscapeImageUrls.Count > 0)
            {
                // PostgreSQL 的数组格式: {"url1","url2","url3"}
                updateData["landscape_image_urls"] = landscapeImageUrls.ToArray();
            }

            updateData["updated_at"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ");

            var jsonContent = System.Text.Json.JsonSerializer.Serialize(updateData);
            Logger.LogInformation("📝 [UpdateImagesDirectAsync] 更新数据: {JsonContent}", jsonContent);

            var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

            // 构建 PATCH 请求 URL
            var requestUrl = $"{supabaseUrl}/rest/v1/cities?id=eq.{cityId}";
            Logger.LogInformation("🌐 [UpdateImagesDirectAsync] 请求 URL: {Url}", requestUrl);

            var response = await httpClient.PatchAsync(requestUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                Logger.LogInformation("✅ [UpdateImagesDirectAsync] 更新成功: StatusCode={StatusCode}, Response={Response}", 
                    response.StatusCode, responseContent);
                return true;
            }
            else
            {
                Logger.LogError("❌ [UpdateImagesDirectAsync] 更新失败: StatusCode={StatusCode}, Response={Response}", 
                    response.StatusCode, responseContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ [UpdateImagesDirectAsync] 异常: CityId={CityId}, Error={Error}", cityId, ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 直接使用 HttpClient 更新城市经纬度，绕过 Postgrest ORM
    /// </summary>
    public async Task<bool> UpdateCoordinatesDirectAsync(Guid cityId, double latitude, double longitude)
    {
        try
        {
            var supabaseUrl = _configuration["Supabase:Url"];
            var supabaseKey = _configuration["Supabase:Key"];

            if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(supabaseKey))
            {
                Logger.LogError("❌ [UpdateCoordinatesDirectAsync] Supabase URL 或 Key 未配置");
                return false;
            }

            Logger.LogInformation(
                "🔄 [UpdateCoordinatesDirectAsync] 开始更新经纬度: CityId={CityId}, Lat={Lat}, Lng={Lng}",
                cityId, latitude, longitude);

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("apikey", supabaseKey);
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {supabaseKey}");
            httpClient.DefaultRequestHeaders.Add("Prefer", "return=representation");

            var updateData = new Dictionary<string, object?>
            {
                ["latitude"] = latitude,
                ["longitude"] = longitude,
                ["updated_at"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ")
            };

            var jsonContent = System.Text.Json.JsonSerializer.Serialize(updateData);
            Logger.LogInformation("📝 [UpdateCoordinatesDirectAsync] 更新数据: {JsonContent}", jsonContent);

            var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

            var requestUrl = $"{supabaseUrl}/rest/v1/cities?id=eq.{cityId}";
            Logger.LogInformation("🌐 [UpdateCoordinatesDirectAsync] 请求 URL: {Url}", requestUrl);

            var response = await httpClient.PatchAsync(requestUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                Logger.LogInformation("✅ [UpdateCoordinatesDirectAsync] 更新成功: CityId={CityId}, StatusCode={StatusCode}",
                    cityId, response.StatusCode);
                return true;
            }
            else
            {
                Logger.LogError("❌ [UpdateCoordinatesDirectAsync] 更新失败: CityId={CityId}, StatusCode={StatusCode}, Response={Response}",
                    cityId, response.StatusCode, responseContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ [UpdateCoordinatesDirectAsync] 异常: CityId={CityId}, Error={Error}", cityId, ex.Message);
            return false;
        }
    }

    public async Task<bool> DeleteAsync(Guid id, Guid? deletedBy = null)
    {
        try
        {
            // 逻辑删除：设置 is_deleted = true, is_active = false
            var now = DateTime.UtcNow;
            await SupabaseClient
                .From<City>()
                .Where(x => x.Id == id)
                .Set(x => x.IsActive, false)
                .Set(x => x.IsDeleted, true)
                .Set(x => x.DeletedAt, now)
                .Set(x => x.DeletedBy, deletedBy)
                .Set(x => x.UpdatedAt, now)
                .Set(x => x.UpdatedById, deletedBy)
                .Update();

            Logger.LogInformation("✅ City 逻辑删除成功，ID: {CityId}, DeletedBy: {DeletedBy}", id, deletedBy);
            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ 删除 City 失败，ID: {CityId}", id);
            return false;
        }
    }

    public async Task<int> GetTotalCountAsync()
    {
        var response = await SupabaseClient
            .From<City>()
            .Filter("is_active", Constants.Operator.Equals, "true")
            .Filter("is_deleted", Constants.Operator.NotEqual, "true")
            .Get();

        return response.Models.Count;
    }

    public async Task<IEnumerable<City>> GetRecommendedAsync(int count)
    {
        var response = await SupabaseClient
            .From<City>()
            .Filter("is_active", Constants.Operator.Equals, "true")
            .Filter("is_deleted", Constants.Operator.NotEqual, "true")
            .Order(x => x.OverallScore!, Constants.Ordering.Descending)
            .Order(x => x.CommunityScore!, Constants.Ordering.Descending)
            .Limit(count)
            .Get();

        return response.Models;
    }

    public async Task<IEnumerable<City>> GetPopularAsync(int limit)
    {
        // 热门城市按照评分、社区活跃度排序
        var response = await SupabaseClient
            .From<City>()
            .Filter("is_active", Constants.Operator.Equals, "true")
            .Filter("is_deleted", Constants.Operator.NotEqual, "true")
            .Order(x => x.OverallScore!, Constants.Ordering.Descending)
            .Order(x => x.CommunityScore!, Constants.Ordering.Descending)
            .Order(x => x.Name, Constants.Ordering.Ascending)
            .Limit(limit)
            .Get();

        return response.Models;
    }

    public async Task<IEnumerable<City>> GetByCountryAsync(string countryName)
    {
        var response = await SupabaseClient
            .From<City>()
            .Filter("is_active", Constants.Operator.Equals, "true")
            .Filter("is_deleted", Constants.Operator.NotEqual, "true")
            .Filter("country", Constants.Operator.ILike, $"%{countryName}%")
            .Order(x => x.Name, Constants.Ordering.Ascending)
            .Get();

        return response.Models;
    }

    public async Task<IEnumerable<City>> GetByCountryIdAsync(Guid countryId)
    {
        var response = await SupabaseClient
            .From<City>()
            .Filter("is_active", Constants.Operator.Equals, "true")
            .Filter("is_deleted", Constants.Operator.NotEqual, "true")
            .Filter("country_id", Constants.Operator.Equals, countryId.ToString())
            .Order(x => x.Name, Constants.Ordering.Ascending)
            .Get();

        return response.Models;
    }

    public async Task<IEnumerable<City>> GetByIdsAsync(IEnumerable<Guid> cityIds)
    {
        var idList = cityIds?.Where(id => id != Guid.Empty).Distinct().ToList();
        if (idList == null || idList.Count == 0) return Enumerable.Empty<City>();

        // Postgrest In operator 需要传递 List，不是字符串
        var response = await SupabaseClient
            .From<City>()
            .Filter("id", Constants.Operator.In, idList)
            .Filter("is_deleted", Constants.Operator.NotEqual, "true")
            .Get();

        return response.Models;
    }
}

/// <summary>
///     用于更新操作的简化 City 类（不包含 Reference 属性）
///     避免 Postgrest 尝试处理外键关系
/// </summary>
[Postgrest.Attributes.Table("cities")]
internal class CityUpdatePayload : Postgrest.Models.BaseModel
{
    [Postgrest.Attributes.PrimaryKey("id")] 
    public Guid Id { get; set; }

    [Postgrest.Attributes.Column("name")] 
    public string Name { get; set; } = string.Empty;

    [Postgrest.Attributes.Column("name_en")] 
    public string? NameEn { get; set; }

    [Postgrest.Attributes.Column("country")] 
    public string Country { get; set; } = string.Empty;

    [Postgrest.Attributes.Column("region")] 
    public string? Region { get; set; }

    [Postgrest.Attributes.Column("description")] 
    public string? Description { get; set; }

    [Postgrest.Attributes.Column("latitude")] 
    public double? Latitude { get; set; }

    [Postgrest.Attributes.Column("longitude")] 
    public double? Longitude { get; set; }

    [Postgrest.Attributes.Column("population")] 
    public int? Population { get; set; }

    [Postgrest.Attributes.Column("climate")] 
    public string? Climate { get; set; }

    [Postgrest.Attributes.Column("timezone")] 
    public string? TimeZone { get; set; }

    [Postgrest.Attributes.Column("currency")] 
    public string? Currency { get; set; }

    [Postgrest.Attributes.Column("image_url")] 
    public string? ImageUrl { get; set; }

    [Postgrest.Attributes.Column("portrait_image_url")] 
    public string? PortraitImageUrl { get; set; }

    [Postgrest.Attributes.Column("landscape_image_urls")] 
    public List<string>? LandscapeImageUrls { get; set; }

    [Postgrest.Attributes.Column("overall_score")] 
    public decimal? OverallScore { get; set; }

    [Postgrest.Attributes.Column("internet_quality_score")] 
    public decimal? InternetQualityScore { get; set; }

    [Postgrest.Attributes.Column("safety_score")] 
    public decimal? SafetyScore { get; set; }

    [Postgrest.Attributes.Column("cost_score")] 
    public decimal? CostScore { get; set; }

    [Postgrest.Attributes.Column("community_score")] 
    public decimal? CommunityScore { get; set; }

    [Postgrest.Attributes.Column("weather_score")] 
    public decimal? WeatherScore { get; set; }

    [Postgrest.Attributes.Column("tags")] 
    public List<string> Tags { get; set; } = new();

    [Postgrest.Attributes.Column("is_active")] 
    public bool IsActive { get; set; } = true;

    [Postgrest.Attributes.Column("updated_at")] 
    public DateTime? UpdatedAt { get; set; }
}

// ============ 城市匹配相关方法 ============

public partial class SupabaseCityRepository
{
    /// <summary>
    ///     按城市名称搜索（支持中英文、模糊匹配）
    /// </summary>
    public async Task<IEnumerable<City>> SearchByNameAsync(
        string name,
        string? countryCode = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Enumerable.Empty<City>();

        try
        {
            var query = SupabaseClient
                .From<City>()
                .Filter("is_active", Constants.Operator.Equals, "true");

            // 如果指定了国家代码，添加国家过滤
            if (!string.IsNullOrWhiteSpace(countryCode))
            {
                query = query.Filter("country_code", Constants.Operator.Equals, countryCode.ToUpperInvariant());
            }

            var response = await query.Get();

            // 在内存中进行名称模糊匹配（支持中英文）
            var cities = response.Models.Where(c =>
                (!string.IsNullOrWhiteSpace(c.Name) &&
                 c.Name.Contains(name, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(c.NameEn) &&
                 c.NameEn.Contains(name, StringComparison.OrdinalIgnoreCase))
            ).ToList();

            Logger.LogInformation(
                "🔍 [SearchByNameAsync] 搜索城市: Name={Name}, CountryCode={CountryCode}, Found={Count}",
                name, countryCode, cities.Count);

            return cities;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ [SearchByNameAsync] 搜索城市失败: Name={Name}", name);
            return Enumerable.Empty<City>();
        }
    }

    /// <summary>
    ///     查找最近的城市（基于经纬度）
    /// </summary>
    public async Task<City?> FindNearestCityAsync(
        double latitude,
        double longitude,
        double maxDistanceKm = 50.0,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 获取所有活跃城市
            var response = await SupabaseClient
                .From<City>()
                .Filter("is_active", Constants.Operator.Equals, "true")
                .Get();

            // 在内存中计算距离并找出最近的城市
            City? nearestCity = null;
            var minDistance = double.MaxValue;

            foreach (var city in response.Models)
            {
                // 跳过没有坐标的城市
                if (!city.Latitude.HasValue || !city.Longitude.HasValue)
                    continue;

                var distance = CalculateDistanceKm(
                    latitude, longitude,
                    city.Latitude.Value, city.Longitude.Value);

                if (distance < minDistance && distance <= maxDistanceKm)
                {
                    minDistance = distance;
                    nearestCity = city;
                }
            }

            if (nearestCity != null)
            {
                Logger.LogInformation(
                    "📍 [FindNearestCityAsync] 找到最近城市: CityId={CityId}, CityName={CityName}, Distance={Distance}km",
                    nearestCity.Id, nearestCity.Name, minDistance);
            }
            else
            {
                Logger.LogInformation(
                    "📍 [FindNearestCityAsync] 未找到 {MaxDistance}km 范围内的城市: Lat={Lat}, Lng={Lng}",
                    maxDistanceKm, latitude, longitude);
            }

            return nearestCity;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ [FindNearestCityAsync] 查找最近城市失败: Lat={Lat}, Lng={Lng}", latitude, longitude);
            return null;
        }
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