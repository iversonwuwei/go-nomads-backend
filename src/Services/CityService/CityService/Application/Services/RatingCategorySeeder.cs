using CityService.Domain.Entities;
using CityService.Domain.Repositories;

namespace CityService.Application.Services;

/// <summary>
/// 评分项数据初始化服务
/// </summary>
public class RatingCategorySeeder
{
    private readonly ICityRatingCategoryRepository _categoryRepository;
    private readonly ILogger<RatingCategorySeeder> _logger;

    public RatingCategorySeeder(
        ICityRatingCategoryRepository categoryRepository,
        ILogger<RatingCategorySeeder> logger)
    {
        _categoryRepository = categoryRepository;
        _logger = logger;
    }

    /// <summary>
    /// 初始化默认评分项
    /// </summary>
    public async Task<RatingCategorySeedResult> SeedDefaultCategoriesAsync()
    {
        var result = new RatingCategorySeedResult();

        try
        {
            // 检查是否已有数据
            var existingCategories = await _categoryRepository.GetAllActiveAsync();
            if (existingCategories.Any())
            {
                _logger.LogInformation("评分项已存在，跳过初始化。现有数量: {Count}", existingCategories.Count);
                result.Success = true;
                result.Message = $"评分项已存在（{existingCategories.Count} 项），无需初始化";
                return result;
            }

            // 默认评分项列表
            var defaultCategories = new List<DefaultCategoryData>
            {
                new() { Name = "生活成本", NameEn = "Cost of Living", Icon = "attach_money", DisplayOrder = 1, Description = "整体生活成本水平" },
                new() { Name = "天气", NameEn = "Weather", Icon = "wb_sunny", DisplayOrder = 2, Description = "气候和天气条件" },
                new() { Name = "交通", NameEn = "Transportation", Icon = "directions_bus", DisplayOrder = 3, Description = "公共交通便利性" },
                new() { Name = "美食", NameEn = "Food", Icon = "restaurant", DisplayOrder = 4, Description = "餐饮质量和多样性" },
                new() { Name = "安全", NameEn = "Safety", Icon = "security", DisplayOrder = 5, Description = "治安和安全状况" },
                new() { Name = "网络", NameEn = "Internet", Icon = "wifi", DisplayOrder = 6, Description = "网络速度和质量" },
                new() { Name = "娱乐", NameEn = "Entertainment", Icon = "local_activity", DisplayOrder = 7, Description = "娱乐和休闲活动" },
                new() { Name = "医疗", NameEn = "Healthcare", Icon = "local_hospital", DisplayOrder = 8, Description = "医疗服务质量" },
                new() { Name = "友好度", NameEn = "Friendliness", Icon = "people", DisplayOrder = 9, Description = "当地人友好程度" },
                new() { Name = "英语水平", NameEn = "English Level", Icon = "language", DisplayOrder = 10, Description = "英语普及程度" }
            };

            foreach (var categoryData in defaultCategories)
            {
                try
                {
                    var category = new CityRatingCategory
                    {
                        Id = Guid.NewGuid(),
                        Name = categoryData.Name,
                        NameEn = categoryData.NameEn,
                        Description = categoryData.Description,
                        Icon = categoryData.Icon,
                        IsDefault = true,
                        CreatedBy = null, // 系统默认项
                        DisplayOrder = categoryData.DisplayOrder,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _categoryRepository.CreateAsync(category);
                    result.CategoriesCreated++;
                    _logger.LogInformation("创建默认评分项: {Name} ({NameEn})", category.Name, category.NameEn);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "创建评分项失败: {Name}", categoryData.Name);
                    result.CategoriesFailed++;
                }
            }

            result.Success = true;
            result.Message = $"成功创建 {result.CategoriesCreated} 个默认评分项";
            _logger.LogInformation("评分项初始化完成: 成功 {Success} 个, 失败 {Failed} 个",
                result.CategoriesCreated, result.CategoriesFailed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "初始化默认评分项失败");
            result.Success = false;
            result.Message = $"初始化失败: {ex.Message}";
        }

        return result;
    }
}

public class DefaultCategoryData
{
    public string Name { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public string? Description { get; set; }
}

public class RatingCategorySeedResult
{
    public bool Success { get; set; }
    public int CategoriesCreated { get; set; }
    public int CategoriesFailed { get; set; }
    public string? Message { get; set; }
}
