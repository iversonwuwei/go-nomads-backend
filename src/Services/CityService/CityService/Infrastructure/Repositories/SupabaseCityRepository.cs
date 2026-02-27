using CityService.Domain.Entities;
using CityService.Domain.Repositories;
using CityService.Domain.ValueObjects;
using CityService.Infrastructure;
using Npgsql;
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
    private readonly string? _connectionString;

    /// <summary>
    ///     列表查询的列投影 — 排除 description、location、landscape_image_urls 等大字段
    /// </summary>
    private const string ListSelectColumns =
        "id,name,name_en,country,country_id,province_id,region," +
        "latitude,longitude,image_url,portrait_image_url," +
        "overall_score,internet_quality_score,safety_score,cost_score,community_score,weather_score," +
        "tags,is_active,created_at";

    public SupabaseCityRepository(Client supabaseClient, ILogger<SupabaseCityRepository> logger, IConfiguration configuration)
        : base(supabaseClient, logger)
    {
        _configuration = configuration;
        // 预解析主机名为 IPv4，避免 IPv6 不可达导致连接失败
        var rawCs = configuration.GetConnectionString("SupabaseDb");
        _connectionString = !string.IsNullOrEmpty(rawCs)
            ? NpgsqlIPv4Helper.ResolveToIPv4(rawCs)
            : rawCs;
    }

    public async Task<IEnumerable<City>> GetAllAsync(int pageNumber, int pageSize)
    {
        var offset = (pageNumber - 1) * pageSize;

        var response = await SupabaseClient
            .From<City>()
            .Select(ListSelectColumns)
            .Filter("is_active", Constants.Operator.Equals, "true")
            .Filter("is_deleted", Constants.Operator.Equals, "false")
            .Order(x => x.OverallScore!, Constants.Ordering.Descending)
            .Range(offset, offset + pageSize - 1)
            .Get();

        return response.Models;
    }

    public async Task<City?> GetByIdAsync(Guid id)
    {
        try
        {
            // 🚀 优化：使用 Npgsql 直接查询，一次性获取所有字段（含 landscape_image_urls）
            // 避免 Postgrest ORM 对 text[] 类型支持不完善导致的双 HTTP 调用
            if (!string.IsNullOrEmpty(_connectionString))
            {
                return await GetByIdWithNpgsqlAsync(id);
            }

            // 降级：使用原始 ORM 方式
            var response = await SupabaseClient
                .From<City>()
                .Where(x => x.Id == id)
                .Filter("is_deleted", Constants.Operator.Equals, "false")
                .Single();

            if (response != null)
            {
                await EnrichCityWithImageUrlsAsync(response);
            }

            return response;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ [GetByIdAsync] 查询城市失败，降级到 ORM: CityId={CityId}", id);

            // Npgsql 失败时降级到 ORM 方式，确保接口不返回 404
            try
            {
                var response = await SupabaseClient
                    .From<City>()
                    .Where(x => x.Id == id)
                    .Filter("is_deleted", Constants.Operator.Equals, "false")
                    .Single();

                if (response != null)
                {
                    await EnrichCityWithImageUrlsAsync(response);
                }

                return response;
            }
            catch (Exception fallbackEx)
            {
                Logger.LogError(fallbackEx, "❌ [GetByIdAsync] ORM 降级也失败: CityId={CityId}", id);
                return null;
            }
        }
    }

    /// <summary>
    /// 使用 Npgsql 直接查询城市详情（性能优化：单次查询替代 ORM + REST 双调用）
    /// 注意：只查 cities 表自身列，不关联 user_city_expenses（cost 由 CacheService 提供）
    /// </summary>
    private async Task<City?> GetByIdWithNpgsqlAsync(Guid id)
    {
        const string sql = @"
            SELECT id, name, name_en, country, country_id, province_id, region,
                   description, location, latitude, longitude, population,
                   climate, timezone, currency,
                   image_url, portrait_image_url, landscape_image_urls,
                   overall_score,
                   internet_quality_score, safety_score, cost_score,
                   community_score, weather_score,
                   tags, is_active, created_at, updated_at,
                   created_by_id, updated_by_id, is_deleted,
                   deleted_at, deleted_by, moderator_id
            FROM cities
            WHERE id = @id AND is_deleted = false
            LIMIT 1";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("id", id);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        var city = new City
        {
            Id = reader.GetGuid(reader.GetOrdinal("id")),
            Name = reader.GetString(reader.GetOrdinal("name")),
            NameEn = reader.IsDBNull(reader.GetOrdinal("name_en")) ? null : reader.GetString(reader.GetOrdinal("name_en")),
            Country = reader.GetString(reader.GetOrdinal("country")),
            CountryId = reader.IsDBNull(reader.GetOrdinal("country_id")) ? null : reader.GetGuid(reader.GetOrdinal("country_id")),
            ProvinceId = reader.IsDBNull(reader.GetOrdinal("province_id")) ? null : reader.GetGuid(reader.GetOrdinal("province_id")),
            Region = reader.IsDBNull(reader.GetOrdinal("region")) ? null : reader.GetString(reader.GetOrdinal("region")),
            Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
            Location = reader.IsDBNull(reader.GetOrdinal("location")) ? null : reader.GetString(reader.GetOrdinal("location")),
            Latitude = reader.IsDBNull(reader.GetOrdinal("latitude")) ? null : reader.GetDouble(reader.GetOrdinal("latitude")),
            Longitude = reader.IsDBNull(reader.GetOrdinal("longitude")) ? null : reader.GetDouble(reader.GetOrdinal("longitude")),
            Population = reader.IsDBNull(reader.GetOrdinal("population")) ? null : reader.GetInt32(reader.GetOrdinal("population")),
            Climate = reader.IsDBNull(reader.GetOrdinal("climate")) ? null : reader.GetString(reader.GetOrdinal("climate")),
            TimeZone = reader.IsDBNull(reader.GetOrdinal("timezone")) ? null : reader.GetString(reader.GetOrdinal("timezone")),
            Currency = reader.IsDBNull(reader.GetOrdinal("currency")) ? null : reader.GetString(reader.GetOrdinal("currency")),
            ImageUrl = reader.IsDBNull(reader.GetOrdinal("image_url")) ? null : reader.GetString(reader.GetOrdinal("image_url")),
            PortraitImageUrl = reader.IsDBNull(reader.GetOrdinal("portrait_image_url")) ? null : reader.GetString(reader.GetOrdinal("portrait_image_url")),
            OverallScore = reader.IsDBNull(reader.GetOrdinal("overall_score")) ? null : reader.GetDecimal(reader.GetOrdinal("overall_score")),
            InternetQualityScore = reader.IsDBNull(reader.GetOrdinal("internet_quality_score")) ? null : reader.GetDecimal(reader.GetOrdinal("internet_quality_score")),
            SafetyScore = reader.IsDBNull(reader.GetOrdinal("safety_score")) ? null : reader.GetDecimal(reader.GetOrdinal("safety_score")),
            CostScore = reader.IsDBNull(reader.GetOrdinal("cost_score")) ? null : reader.GetDecimal(reader.GetOrdinal("cost_score")),
            CommunityScore = reader.IsDBNull(reader.GetOrdinal("community_score")) ? null : reader.GetDecimal(reader.GetOrdinal("community_score")),
            WeatherScore = reader.IsDBNull(reader.GetOrdinal("weather_score")) ? null : reader.GetDecimal(reader.GetOrdinal("weather_score")),
            IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            UpdatedAt = reader.IsDBNull(reader.GetOrdinal("updated_at")) ? null : reader.GetDateTime(reader.GetOrdinal("updated_at")),
            CreatedById = reader.IsDBNull(reader.GetOrdinal("created_by_id")) ? null : reader.GetGuid(reader.GetOrdinal("created_by_id")),
            UpdatedById = reader.IsDBNull(reader.GetOrdinal("updated_by_id")) ? null : reader.GetGuid(reader.GetOrdinal("updated_by_id")),
            IsDeleted = reader.GetBoolean(reader.GetOrdinal("is_deleted")),
            DeletedAt = reader.IsDBNull(reader.GetOrdinal("deleted_at")) ? null : reader.GetDateTime(reader.GetOrdinal("deleted_at")),
            DeletedBy = reader.IsDBNull(reader.GetOrdinal("deleted_by")) ? null : reader.GetGuid(reader.GetOrdinal("deleted_by")),
            ModeratorId = reader.IsDBNull(reader.GetOrdinal("moderator_id")) ? null : reader.GetGuid(reader.GetOrdinal("moderator_id"))
        };

        // 直接从 Npgsql 读取 text[] 数组 — 无需额外 HTTP 调用
        var landscapeOrdinal = reader.GetOrdinal("landscape_image_urls");
        if (!reader.IsDBNull(landscapeOrdinal))
        {
            var landscapeUrls = reader.GetFieldValue<string[]>(landscapeOrdinal);
            city.LandscapeImageUrls = landscapeUrls?.ToList();
        }

        // 读取 tags 数组
        var tagsOrdinal = reader.GetOrdinal("tags");
        if (!reader.IsDBNull(tagsOrdinal))
        {
            var tags = reader.GetFieldValue<string[]>(tagsOrdinal);
            city.Tags = tags?.ToList() ?? new List<string>();
        }

        Logger.LogDebug(
            "🚀 [GetByIdWithNpgsql] 单次查询获取城市: Id={Id}, Name={Name}, LandscapeCount={Count}",
            city.Id, city.Name, city.LandscapeImageUrls?.Count ?? 0);

        return city;
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
        // 🚀 优化：当有搜索词时，使用 Npgsql 直接执行多字段搜索（name + name_en + country）
        if (!string.IsNullOrWhiteSpace(criteria.Name) && !string.IsNullOrEmpty(_connectionString))
        {
            return await SearchWithNpgsqlAsync(criteria);
        }

        var offset = (criteria.PageNumber - 1) * criteria.PageSize;

        var query = SupabaseClient
            .From<City>()
            .Select(ListSelectColumns)
            .Filter("is_active", Constants.Operator.Equals, "true")
            .Filter("is_deleted", Constants.Operator.Equals, "false");

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

    /// <summary>
    /// 使用 Npgsql 执行多字段模糊搜索（name + name_en + country），利用 pg_trgm 索引
    /// </summary>
    private async Task<IEnumerable<City>> SearchWithNpgsqlAsync(CitySearchCriteria criteria)
    {
        var offset = (criteria.PageNumber - 1) * criteria.PageSize;
        var query = criteria.Name!.Trim();

        // 使用列投影（与 ORM 查询一致）避免返回 description 等大字段
        // 注意：不通过子查询关联 user_city_expenses，cost 过滤使用 overall_score 替代
        const string sql = @"
            SELECT id, name, name_en, country, country_id, province_id, region,
                   latitude, longitude, image_url, portrait_image_url,
                   overall_score, internet_quality_score, safety_score,
                   cost_score, community_score, weather_score,
                   tags, is_active, created_at
            FROM cities
            WHERE is_active = true
              AND is_deleted = false
              AND (
                name ILIKE @pattern
                OR name_en ILIKE @pattern
                OR country ILIKE @pattern
              )
              AND (@country IS NULL OR country ILIKE @countryPattern)
              AND (@region IS NULL OR region = @region)
              AND (@minScore IS NULL OR overall_score >= @minScore)
            ORDER BY
              GREATEST(
                similarity(name, @query),
                similarity(COALESCE(name_en, ''), @query)
              ) DESC,
              overall_score DESC NULLS LAST
            LIMIT @pageSize OFFSET @offset";

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var cmd = new NpgsqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("pattern", $"%{query}%");
        cmd.Parameters.AddWithValue("query", query);
        cmd.Parameters.AddWithValue("country", (object?)criteria.Country ?? DBNull.Value);
        cmd.Parameters.AddWithValue("countryPattern", criteria.Country != null ? $"%{criteria.Country}%" : (object)DBNull.Value);
        cmd.Parameters.AddWithValue("region", (object?)criteria.Region ?? DBNull.Value);
        cmd.Parameters.AddWithValue("minScore", criteria.MinScore.HasValue ? (object)criteria.MinScore.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("pageSize", criteria.PageSize);
        cmd.Parameters.AddWithValue("offset", offset);

        var cities = new List<City>();
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var city = new City
            {
                Id = reader.GetGuid(reader.GetOrdinal("id")),
                Name = reader.GetString(reader.GetOrdinal("name")),
                NameEn = reader.IsDBNull(reader.GetOrdinal("name_en")) ? null : reader.GetString(reader.GetOrdinal("name_en")),
                Country = reader.GetString(reader.GetOrdinal("country")),
                CountryId = reader.IsDBNull(reader.GetOrdinal("country_id")) ? null : reader.GetGuid(reader.GetOrdinal("country_id")),
                ProvinceId = reader.IsDBNull(reader.GetOrdinal("province_id")) ? null : reader.GetGuid(reader.GetOrdinal("province_id")),
                Region = reader.IsDBNull(reader.GetOrdinal("region")) ? null : reader.GetString(reader.GetOrdinal("region")),
                Latitude = reader.IsDBNull(reader.GetOrdinal("latitude")) ? null : reader.GetDouble(reader.GetOrdinal("latitude")),
                Longitude = reader.IsDBNull(reader.GetOrdinal("longitude")) ? null : reader.GetDouble(reader.GetOrdinal("longitude")),
                ImageUrl = reader.IsDBNull(reader.GetOrdinal("image_url")) ? null : reader.GetString(reader.GetOrdinal("image_url")),
                PortraitImageUrl = reader.IsDBNull(reader.GetOrdinal("portrait_image_url")) ? null : reader.GetString(reader.GetOrdinal("portrait_image_url")),
                OverallScore = reader.IsDBNull(reader.GetOrdinal("overall_score")) ? null : reader.GetDecimal(reader.GetOrdinal("overall_score")),
                InternetQualityScore = reader.IsDBNull(reader.GetOrdinal("internet_quality_score")) ? null : reader.GetDecimal(reader.GetOrdinal("internet_quality_score")),
                SafetyScore = reader.IsDBNull(reader.GetOrdinal("safety_score")) ? null : reader.GetDecimal(reader.GetOrdinal("safety_score")),
                CostScore = reader.IsDBNull(reader.GetOrdinal("cost_score")) ? null : reader.GetDecimal(reader.GetOrdinal("cost_score")),
                CommunityScore = reader.IsDBNull(reader.GetOrdinal("community_score")) ? null : reader.GetDecimal(reader.GetOrdinal("community_score")),
                WeatherScore = reader.IsDBNull(reader.GetOrdinal("weather_score")) ? null : reader.GetDecimal(reader.GetOrdinal("weather_score")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
            };

            // 读取 tags 数组
            var tagsOrdinal = reader.GetOrdinal("tags");
            if (!reader.IsDBNull(tagsOrdinal))
            {
                var tags = reader.GetFieldValue<string[]>(tagsOrdinal);
                city.Tags = tags?.ToList() ?? new List<string>();
            }

            cities.Add(city);
        }

        // 标签过滤在内存中进行
        IEnumerable<City> result = cities;
        if (criteria.Tags is { Count: > 0 })
        {
            result = cities.Where(c => c.Tags != null && criteria.Tags.All(tag => c.Tags.Contains(tag)));
        }

        Logger.LogInformation(
            "🔍 [SearchWithNpgsql] 多字段搜索完成: query={Query}, 结果数={Count}",
            query, cities.Count);

        return result.ToList();
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
        try
        {
            // 使用 RPC 函数在数据库端计数，避免全表加载到内存
            var result = await SupabaseClient.Rpc("get_city_total_count", null);
            if (result?.Content != null && int.TryParse(result.Content.Trim('"'), out var count))
            {
                return count;
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "⚠️ [GetTotalCountAsync] RPC 调用失败，回退到 Select count");
        }

        // 回退方案：仅查 id 列计数
        var response = await SupabaseClient
            .From<City>()
            .Filter("is_active", Constants.Operator.Equals, "true")
            .Filter("is_deleted", Constants.Operator.Equals, "false")
            .Select("id")
            .Get();

        return response.Models.Count;
    }

    public async Task<IEnumerable<City>> GetRecommendedAsync(int count)
    {
        var response = await SupabaseClient
            .From<City>()
            .Select(ListSelectColumns)
            .Filter("is_active", Constants.Operator.Equals, "true")
            .Filter("is_deleted", Constants.Operator.Equals, "false")
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
            .Select(ListSelectColumns)
            .Filter("is_active", Constants.Operator.Equals, "true")
            .Filter("is_deleted", Constants.Operator.Equals, "false")
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
            .Select(ListSelectColumns)
            .Filter("is_active", Constants.Operator.Equals, "true")
            .Filter("is_deleted", Constants.Operator.Equals, "false")
            .Filter("country", Constants.Operator.ILike, $"%{countryName}%")
            .Order(x => x.Name, Constants.Ordering.Ascending)
            .Get();

        return response.Models;
    }

    public async Task<IEnumerable<City>> GetByCountryIdAsync(Guid countryId)
    {
        var response = await SupabaseClient
            .From<City>()
            .Select(ListSelectColumns)
            .Filter("is_active", Constants.Operator.Equals, "true")
            .Filter("is_deleted", Constants.Operator.Equals, "false")
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
            .Filter("is_deleted", Constants.Operator.Equals, "false")
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
    ///     🚀 优化：使用 ILIKE 在数据库端过滤，利用 trgm 索引，替代全表加载到内存
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
            // 优先使用 RPC 函数（支持 name + name_en 双字段搜索）
            try
            {
                var rpcResult = await SupabaseClient.Rpc("search_cities_by_name",
                    new Dictionary<string, object>
                    {
                        { "p_name", name },
                        { "p_country_code", countryCode ?? (object)DBNull.Value }
                    });

                if (rpcResult?.Content != null)
                {
                    var cities = System.Text.Json.JsonSerializer.Deserialize<List<City>>(rpcResult.Content,
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (cities != null)
                    {
                        Logger.LogInformation(
                            "🔍 [SearchByNameAsync] RPC 搜索城市: Name={Name}, Found={Count}",
                            name, cities.Count);
                        return cities;
                    }
                }
            }
            catch (Exception rpcEx)
            {
                Logger.LogWarning(rpcEx, "⚠️ [SearchByNameAsync] RPC 调用失败，回退到 ILIKE 查询");
            }

            // 回退方案：使用 ILIKE 在 name 字段搜索
            var query = SupabaseClient
                .From<City>()
                .Filter("is_active", Constants.Operator.Equals, "true")
                .Filter("is_deleted", Constants.Operator.Equals, "false")
                .Filter("name", Constants.Operator.ILike, $"%{name}%");

            if (!string.IsNullOrWhiteSpace(countryCode))
            {
                query = query.Filter("country", Constants.Operator.Equals, countryCode.ToUpperInvariant());
            }

            var response = await query
                .Order(x => x.OverallScore!, Constants.Ordering.Descending)
                .Limit(50)
                .Get();

            Logger.LogInformation(
                "🔍 [SearchByNameAsync] ILIKE 搜索城市: Name={Name}, CountryCode={CountryCode}, Found={Count}",
                name, countryCode, response.Models.Count);

            return response.Models;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ [SearchByNameAsync] 搜索城市失败: Name={Name}", name);
            return Enumerable.Empty<City>();
        }
    }

    /// <summary>
    ///     查找最近的城市（基于经纬度）
    ///     🚀 优化：使用 PostGIS RPC 函数 + GIST 索引，替代全表加载 + C# Haversine 计算
    /// </summary>
    public async Task<City?> FindNearestCityAsync(
        double latitude,
        double longitude,
        double maxDistanceKm = 50.0,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 优先使用 PostGIS RPC 函数（利用 GIST 索引）
            try
            {
                var rpcResult = await SupabaseClient.Rpc("find_nearest_city",
                    new Dictionary<string, object>
                    {
                        { "p_lat", latitude },
                        { "p_lng", longitude },
                        { "p_max_distance_km", maxDistanceKm }
                    });

                if (rpcResult?.Content != null)
                {
                    var resultArray = System.Text.Json.JsonDocument.Parse(rpcResult.Content).RootElement;
                    if (resultArray.ValueKind == System.Text.Json.JsonValueKind.Array && resultArray.GetArrayLength() > 0)
                    {
                        var first = resultArray[0];
                        var cityId = Guid.Parse(first.GetProperty("city_id").GetString()!);
                        var distanceKm = first.GetProperty("distance_km").GetDouble();

                        // 用 ID 获取完整城市对象
                        var city = await GetByIdAsync(cityId);
                        if (city != null)
                        {
                            Logger.LogInformation(
                                "📍 [FindNearestCityAsync] PostGIS 找到最近城市: CityId={CityId}, CityName={CityName}, Distance={Distance}km",
                                city.Id, city.Name, distanceKm);
                            return city;
                        }
                    }
                }
                
                Logger.LogInformation(
                    "📍 [FindNearestCityAsync] PostGIS 未找到 {MaxDistance}km 范围内的城市",
                    maxDistanceKm);
                return null;
            }
            catch (Exception rpcEx)
            {
                Logger.LogWarning(rpcEx, "⚠️ [FindNearestCityAsync] RPC 调用失败，回退到内存计算");
            }

            // 回退方案：内存计算（仅在 RPC 不可用时）
            var response = await SupabaseClient
                .From<City>()
                .Filter("is_active", Constants.Operator.Equals, "true")
                .Select("id,name,latitude,longitude")
                .Get();

            City? nearestCity = null;
            var minDistance = double.MaxValue;

            foreach (var city in response.Models)
            {
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
                // 需要获取完整城市数据
                nearestCity = await GetByIdAsync(nearestCity.Id) ?? nearestCity;
                Logger.LogInformation(
                    "📍 [FindNearestCityAsync] 内存计算找到最近城市: CityId={CityId}, CityName={CityName}, Distance={Distance}km",
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

    /// <summary>
    /// 获取所有不同的区域（大洲）列表
    /// </summary>
    public async Task<IEnumerable<string>> GetDistinctRegionsAsync()
    {
        try
        {
            var response = await SupabaseClient
                .From<City>()
                .Filter("is_active", Constants.Operator.Equals, "true")
                .Filter("is_deleted", Constants.Operator.Equals, "false")
                .Select("region")
                .Get();

            var regions = response.Models
                .Where(c => !string.IsNullOrWhiteSpace(c.Region))
                .Select(c => c.Region!)
                .Distinct()
                .OrderBy(r => r)
                .ToList();

            Logger.LogInformation("🌍 [GetDistinctRegionsAsync] 获取到 {Count} 个区域", regions.Count);
            return regions;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ [GetDistinctRegionsAsync] 获取区域列表失败");
            return Enumerable.Empty<string>();
        }
    }

    /// <summary>
    /// 根据区域获取城市列表（分页）
    /// </summary>
    public async Task<IEnumerable<City>> GetByRegionAsync(string region, int pageNumber, int pageSize)
    {
        var offset = (pageNumber - 1) * pageSize;

        var response = await SupabaseClient
            .From<City>()
            .Select(ListSelectColumns)
            .Filter("is_active", Constants.Operator.Equals, "true")
            .Filter("is_deleted", Constants.Operator.Equals, "false")
            .Filter("region", Constants.Operator.ILike, $"%{region}%")
            .Order(x => x.OverallScore!, Constants.Ordering.Descending)
            .Range(offset, offset + pageSize - 1)
            .Get();

        return response.Models;
    }

    /// <summary>
    /// 获取某区域的城市总数
    /// </summary>
    public async Task<int> GetCountByRegionAsync(string region)
    {
        try
        {
            var response = await SupabaseClient
                .From<City>()
                .Filter("is_active", Constants.Operator.Equals, "true")
                .Filter("is_deleted", Constants.Operator.Equals, "false")
                .Filter("region", Constants.Operator.ILike, $"%{region}%")
                .Select("id")
                .Get();

            return response.Models.Count;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ [GetCountByRegionAsync] 获取区域城市数量失败: {Region}", region);
            return 0;
        }
    }

    /// <summary>
    /// 根据多个国家ID获取城市列表（分页）
    /// </summary>
    public async Task<IEnumerable<City>> GetByCountryIdsAsync(IEnumerable<Guid> countryIds, int pageNumber, int pageSize)
    {
        var idList = countryIds.ToList();
        if (idList.Count == 0) return Enumerable.Empty<City>();

        var offset = (pageNumber - 1) * pageSize;

        // Supabase Postgrest 的 In 操作需要将 Guid 转为字符串列表
        var idStrings = idList.Select(id => id.ToString()).ToList();

        var response = await SupabaseClient
            .From<City>()
            .Select(ListSelectColumns)
            .Filter("is_active", Constants.Operator.Equals, "true")
            .Filter("is_deleted", Constants.Operator.Equals, "false")
            .Filter("country_id", Constants.Operator.In, idStrings)
            .Order(x => x.OverallScore!, Constants.Ordering.Descending)
            .Range(offset, offset + pageSize - 1)
            .Get();

        return response.Models;
    }

    /// <summary>
    /// 根据多个国家ID获取城市总数
    /// </summary>
    public async Task<int> GetCountByCountryIdsAsync(IEnumerable<Guid> countryIds)
    {
        try
        {
            var idList = countryIds.ToList();
            if (idList.Count == 0) return 0;

            var idStrings = idList.Select(id => id.ToString()).ToList();

            var response = await SupabaseClient
                .From<City>()
                .Filter("is_active", Constants.Operator.Equals, "true")
                .Filter("is_deleted", Constants.Operator.Equals, "false")
                .Filter("country_id", Constants.Operator.In, idStrings)
                .Select("id")
                .Get();

            return response.Models.Count;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ [GetCountByCountryIdsAsync] 获取国家城市数量失败");
            return 0;
        }
    }

    /// <summary>
    /// 根据大洲筛选城市（同时支持 country_id 和 country name 匹配）
    /// </summary>
    public async Task<IEnumerable<City>> GetByContinentAsync(IEnumerable<Guid> countryIds, IEnumerable<string> countryNames, int pageNumber, int pageSize)
    {
        var idList = countryIds.ToList();
        var nameList = countryNames.ToList();
        if (idList.Count == 0 && nameList.Count == 0) return Enumerable.Empty<City>();

        var offset = (pageNumber - 1) * pageSize;

        // Supabase Postgrest 不支持 OR 条件，所以分两次查询后合并
        var allCities = new List<City>();

        if (idList.Count > 0)
        {
            var idStrings = idList.Select(id => id.ToString()).ToList();
            var byIdResponse = await SupabaseClient
                .From<City>()
                .Filter("is_active", Constants.Operator.Equals, "true")
                .Filter("is_deleted", Constants.Operator.Equals, "false")
                .Filter("country_id", Constants.Operator.In, idStrings)
                .Order(x => x.OverallScore!, Constants.Ordering.Descending)
                .Get();
            allCities.AddRange(byIdResponse.Models);
        }

        if (nameList.Count > 0)
        {
            var byNameResponse = await SupabaseClient
                .From<City>()
                .Filter("is_active", Constants.Operator.Equals, "true")
                .Filter("is_deleted", Constants.Operator.Equals, "false")
                .Filter("country", Constants.Operator.In, nameList)
                .Order(x => x.OverallScore!, Constants.Ordering.Descending)
                .Get();
            
            // 合并去重（按ID）
            var existingIds = new HashSet<Guid>(allCities.Select(c => c.Id));
            foreach (var city in byNameResponse.Models)
            {
                if (!existingIds.Contains(city.Id))
                {
                    allCities.Add(city);
                }
            }
        }

        // 排序后分页
        return allCities
            .OrderByDescending(c => c.OverallScore ?? 0)
            .Skip(offset)
            .Take(pageSize)
            .ToList();
    }

    /// <summary>
    /// 获取所有活跃城市的简要信息（仅 id, country_id, country）
    /// 用于内存中批量统计，避免多次数据库查询
    /// </summary>
    public async Task<IEnumerable<City>> GetAllActiveCityBriefAsync()
    {
        try
        {
            var response = await SupabaseClient
                .From<City>()
                .Filter("is_active", Constants.Operator.Equals, "true")
                .Filter("is_deleted", Constants.Operator.Equals, "false")
                .Select("id,country_id,country")
                .Get();

            return response.Models;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ [GetAllActiveCityBriefAsync] 获取城市简要信息失败");
            return Enumerable.Empty<City>();
        }
    }

    /// <summary>
    /// 根据大洲统计城市总数（同时支持 country_id 和 country name 匹配）
    /// </summary>
    public async Task<int> GetCountByContinentAsync(IEnumerable<Guid> countryIds, IEnumerable<string> countryNames)
    {
        try
        {
            var idList = countryIds.ToList();
            var nameList = countryNames.ToList();
            if (idList.Count == 0 && nameList.Count == 0) return 0;

            var allIds = new HashSet<Guid>();

            if (idList.Count > 0)
            {
                var idStrings = idList.Select(id => id.ToString()).ToList();
                var byIdResponse = await SupabaseClient
                    .From<City>()
                    .Filter("is_active", Constants.Operator.Equals, "true")
                    .Filter("is_deleted", Constants.Operator.Equals, "false")
                    .Filter("country_id", Constants.Operator.In, idStrings)
                    .Select("id")
                    .Get();
                foreach (var c in byIdResponse.Models) allIds.Add(c.Id);
            }

            if (nameList.Count > 0)
            {
                var byNameResponse = await SupabaseClient
                    .From<City>()
                    .Filter("is_active", Constants.Operator.Equals, "true")
                    .Filter("is_deleted", Constants.Operator.Equals, "false")
                    .Filter("country", Constants.Operator.In, nameList)
                    .Select("id")
                    .Get();
                foreach (var c in byNameResponse.Models) allIds.Add(c.Id);
            }

            return allIds.Count;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "❌ [GetCountByContinentAsync] 获取大洲城市数量失败");
            return 0;
        }
    }
}