# UserCityReview 后端字段修改完成

## 修改时间
2025-10-31

## 修改原因
前端 Flutter 应用的 `UserCityReview` 模型使用了 `title`, `content`, `visitDate` 字段,并且这些字段在 UI 中被实际使用:

### 前端使用证据:

1. **UI 显示** (city_detail_page.dart):
```dart
Text(review.title, ...),           // 显示标题
Text(review.content, ...),         // 显示内容
if (review.visitDate != null)      // 显示访问日期
  Text('Visited ${_formatDate(review.visitDate!)}', ...)
```

2. **表单提交** (add_review_page.dart):
```dart
Get.back(result: {
  'rating': _rating.value,
  'title': _titleController.text.trim(),    // 标题输入
  'content': _contentController.text.trim(), // 内容输入
});
```

前端已经实现了完整的评论创建表单,包括标题和内容的输入框。

---

## 修改内容

### 1. Domain Entity (Domain/Entities/UserCityReview.cs)

**添加了 3 个新字段:**

```csharp
/// <summary>
/// 评论标题
/// </summary>
[Required]
[MaxLength(200)]
[Column("title")]
public string Title { get; set; } = string.Empty;

/// <summary>
/// 评论内容
/// </summary>
[Required]
[MaxLength(2000)]
[Column("content")]
public string Content { get; set; } = string.Empty;

/// <summary>
/// 访问日期(可选)
/// </summary>
[Column("visit_date")]
public DateTime? VisitDate { get; set; }
```

**保留了原有字段:**
- `ReviewText` - 保留用于向后兼容或其他用途
- `InternetQualityScore`, `SafetyScore`, `CostScore`, `CommunityScore`, `WeatherScore` - 详细评分字段

---

### 2. Application DTO (Application/DTOs/UserCityContentDTOs.cs)

**更新了 UserCityReviewDto:**

```csharp
public class UserCityReviewDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string CityId { get; set; } = string.Empty;
    public int Rating { get; set; }
    
    // ✅ 新增字段
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime? VisitDate { get; set; }
    
    // 保留原有字段
    public string? ReviewText { get; set; }
    public int? InternetQualityScore { get; set; }
    public int? SafetyScore { get; set; }
    public int? CostScore { get; set; }
    public int? CommunityScore { get; set; }
    public int? WeatherScore { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

**更新了 UpsertCityReviewRequest:**

```csharp
public class UpsertCityReviewRequest
{
    [Required]
    public string CityId { get; set; } = string.Empty;

    [Required]
    [Range(1, 5)]
    public int Rating { get; set; }

    // ✅ 新增必填字段
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Content { get; set; } = string.Empty;

    public DateTime? VisitDate { get; set; }

    // 保留原有字段
    [MaxLength(2000)]
    public string? ReviewText { get; set; }

    [Range(1, 5)]
    public int? InternetQualityScore { get; set; }

    [Range(1, 5)]
    public int? SafetyScore { get; set; }

    [Range(1, 5)]
    public int? CostScore { get; set; }

    [Range(1, 5)]
    public int? CommunityScore { get; set; }

    [Range(1, 5)]
    public int? WeatherScore { get; set; }
}
```

---

### 3. Application Service (Application/Services/UserCityContentApplicationService.cs)

**更新了 UpsertReviewAsync 方法:**

```csharp
public async Task<UserCityReviewDto> UpsertReviewAsync(Guid userId, UpsertCityReviewRequest request)
{
    var review = new UserCityReview
    {
        UserId = userId,
        CityId = request.CityId,
        Rating = request.Rating,
        
        // ✅ 映射新字段
        Title = request.Title,
        Content = request.Content,
        VisitDate = request.VisitDate,
        
        // 保留原有字段映射
        ReviewText = request.ReviewText,
        InternetQualityScore = request.InternetQualityScore,
        SafetyScore = request.SafetyScore,
        CostScore = request.CostScore,
        CommunityScore = request.CommunityScore,
        WeatherScore = request.WeatherScore
    };

    var upserted = await _reviewRepository.UpsertAsync(review);
    _logger.LogInformation("用户 {UserId} 更新了城市 {CityId} 的评论", userId, request.CityId);

    return MapReviewToDto(upserted);
}
```

**更新了 MapReviewToDto 方法:**

```csharp
private static UserCityReviewDto MapReviewToDto(UserCityReview review)
{
    return new UserCityReviewDto
    {
        Id = review.Id,
        UserId = review.UserId,
        CityId = review.CityId,
        Rating = review.Rating,
        
        // ✅ 映射新字段
        Title = review.Title,
        Content = review.Content,
        VisitDate = review.VisitDate,
        
        // 保留原有字段映射
        ReviewText = review.ReviewText,
        InternetQualityScore = review.InternetQualityScore,
        SafetyScore = review.SafetyScore,
        CostScore = review.CostScore,
        CommunityScore = review.CommunityScore,
        WeatherScore = review.WeatherScore,
        CreatedAt = review.CreatedAt,
        UpdatedAt = review.UpdatedAt
    };
}
```

---

## ✅ 验证结果

### 编译状态:
```
PS E:\Workspaces\WaldenProjects\go-nomads\src\Services\CityService\CityService> dotnet build
还原完成(0.4)
  Shared 已成功 (0.3 秒)
  CityService 已成功 (2.8 秒)

在 4.2 秒内生成 已成功
```

**✅ 编译通过,无错误!**

---

## 🔄 数据库迁移需求

### ⚠️ 需要添加数据库字段:

在 Supabase 的 `user_city_reviews` 表中添加以下列:

```sql
-- 添加新字段到 user_city_reviews 表
ALTER TABLE user_city_reviews
  ADD COLUMN title VARCHAR(200) NOT NULL DEFAULT '',
  ADD COLUMN content TEXT NOT NULL DEFAULT '',
  ADD COLUMN visit_date TIMESTAMP WITH TIME ZONE;

-- 可选:从现有 review_text 迁移数据到 content (如果有数据)
UPDATE user_city_reviews
SET content = COALESCE(review_text, '')
WHERE content = '' AND review_text IS NOT NULL;
```

### 字段说明:
| 字段名 | 类型 | 约束 | 说明 |
|--------|------|------|------|
| `title` | VARCHAR(200) | NOT NULL | 评论标题 |
| `content` | TEXT | NOT NULL | 评论内容 |
| `visit_date` | TIMESTAMP | NULLABLE | 访问日期(可选) |

---

## 📋 前后端字段对照表

| 前端 (Dart) | 后端 (C#) | 数据库列名 | 类型 | 必填 |
|-------------|-----------|-----------|------|------|
| `id` | `Id` | `id` | UUID | ✅ |
| `userId` | `UserId` | `user_id` | UUID | ✅ |
| `cityId` | `CityId` | `city_id` | String | ✅ |
| `rating` | `Rating` | `rating` | int | ✅ |
| `title` | `Title` | `title` | String(200) | ✅ |
| `content` | `Content` | `content` | Text | ✅ |
| `visitDate` | `VisitDate` | `visit_date` | DateTime? | ❌ |
| `createdAt` | `CreatedAt` | `created_at` | DateTime | ✅ |
| `updatedAt` | `UpdatedAt` | `updated_at` | DateTime? | ❌ |

### 保留的额外字段(前端未使用):
- `reviewText` (可用于其他用途)
- `internetQualityScore`, `safetyScore`, `costScore`, `communityScore`, `weatherScore` (详细评分)

---

## 🎯 下一步操作

1. **✅ 已完成 - 后端代码修改**
   - Domain Entity 更新
   - DTOs 更新
   - Application Service 映射更新
   - 编译验证通过

2. **⏳ 待执行 - 数据库迁移**
   ```bash
   # 需要在 Supabase 中执行 SQL 脚本
   # 或者创建数据库迁移文件
   ```

3. **⏳ 待测试 - API 集成测试**
   - 测试 POST /api/v1/cities/{cityId}/user-content/reviews (创建评论)
   - 测试 GET /api/v1/cities/{cityId}/user-content/reviews (获取评论列表)
   - 验证新字段正确保存和返回
   - 从 Flutter 应用测试完整流程

4. **⏳ 可选 - 重启服务**
   ```bash
   cd E:\Workspaces\WaldenProjects\go-nomads\deployment
   .\deploy-services-local.ps1
   # 或
   docker-compose restart cityservice
   ```

---

## 📝 兼容性说明

### 向后兼容:
- ✅ 保留了 `ReviewText` 字段,不影响现有数据
- ✅ 保留了所有详细评分字段
- ✅ 新字段设置为必填,确保数据完整性

### 前端匹配:
- ✅ `title`, `content`, `visitDate` 字段完全匹配前端模型
- ✅ UI 可以正确显示评论标题和内容
- ✅ 表单提交的数据会被正确接收

---

## ✨ 总结

后端已成功修改以匹配前端的 Review 数据结构,现在:
- 前端发送的 `title`, `content`, `visitDate` 会被正确处理
- API 返回的数据包含前端需要的所有字段
- UI 可以正确显示评论的标题、内容和访问日期

**需要执行数据库迁移后,整个评论功能将完全可用!**
