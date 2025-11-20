# Pros & Cons 投票功能实现指南

本文档说明如何在 CityService 中实现 Pros & Cons 投票功能的 Service 和 Repository 层。

## 已完成部分

1. ✅ SQL Schema (`go-nomads/db/pros_cons_schema.sql`)
2. ✅ DTO 定义 (`VoteProsConsRequest`)
3. ✅ Controller 层 (`ProsConsVoteController`)
4. ✅ Service 接口 (`IUserCityContentService.VoteProsConsAsync`)

## 待实现部分

### 1. Service 层实现

在 `CityService/Services/UserCityContentService.cs` 中添加:

```csharp
public async Task VoteProsConsAsync(Guid userId, Guid prosConsId, bool isUpvote)
{
    // 1. 验证 pros_cons 记录存在且未被删除
    var prosCons = await _supabaseClient
        .From<CityProsCons>()
        .Where(x => x.Id == prosConsId && x.Status == "active")
        .Single();

    if (prosCons == null)
    {
        throw new InvalidOperationException("该条目不存在或已被删除");
    }

    // 2. 检查用户是否已投票（通过唯一索引会自动阻止重复，但可以提前检查给出友好提示）
    var existingVote = await _supabaseClient
        .From<CityProsConsVote>()
        .Where(x => x.ProsConsId == prosConsId && x.VoterUserId == userId)
        .Single();

    if (existingVote != null)
    {
        throw new InvalidOperationException("你已经为该条目投过票啦");
    }

    // 3. 插入投票记录（触发器会自动更新 upvotes/downvotes）
    var vote = new CityProsConsVote
    {
        Id = Guid.NewGuid(),
        ProsConsId = prosConsId,
        VoterUserId = userId,
        IsUpvote = isUpvote,
        CreatedAt = DateTime.UtcNow
    };

    try
    {
        await _supabaseClient
            .From<CityProsConsVote>()
            .Insert(vote);
    }
    catch (Exception ex) when (ex.Message.Contains("duplicate key") || ex.Message.Contains("unique constraint"))
    {
        // 如果前置检查漏过（并发场景），捕获数据库唯一约束错误
        throw new InvalidOperationException("你已经为该条目投过票啦");
    }
}
```

### 2. Domain Entity

在 `CityService/Domain/Entities/` 中添加:

```csharp
/// <summary>
/// Pros & Cons 投票记录实体
/// </summary>
[Table("city_pros_cons_votes")]
public class CityProsConsVote
{
    [PrimaryKey("id")]
    public Guid Id { get; set; }

    [Column("pros_cons_id")]
    public Guid ProsConsId { get; set; }

    [Column("voter_user_id")]
    public Guid VoterUserId { get; set; }

    [Column("is_upvote")]
    public bool IsUpvote { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
```

如果已有 `CityProsCons` 实体，确保包含:

```csharp
[Table("city_pros_cons")]
public class CityProsCons
{
    [PrimaryKey("id")]
    public Guid Id { get; set; }

    [Column("city_id")]
    public Guid CityId { get; set; }

    [Column("author_user_id")]
    public Guid AuthorUserId { get; set; }

    [Column("text")]
    public string Text { get; set; } = string.Empty;

    [Column("is_pro")]
    public bool IsPro { get; set; }

    [Column("upvotes")]
    public int Upvotes { get; set; }

    [Column("downvotes")]
    public int Downvotes { get; set; }

    [Column("status")]
    public string Status { get; set; } = "active"; // active | deleted

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
```

### 3. 数据库触发器验证

确保 `pros_cons_schema.sql` 中的触发器已正确执行:

```sql
-- 查询触发器是否存在
SELECT trigger_name, event_manipulation, event_object_table, action_statement
FROM information_schema.triggers
WHERE trigger_name = 'city_pros_cons_votes_ai';
```

### 4. API 测试

使用以下 PowerShell 脚本测试投票功能:

```powershell
# test-pros-cons-vote.ps1

$baseUrl = "https://your-api-gateway/api/v1"
$token = "your-jwt-token"

# 1. 获取某个城市的 pros/cons 列表
$cityId = "your-city-id"
$response = Invoke-RestMethod -Uri "$baseUrl/cities/$cityId/user-content/pros-cons" `
    -Method Get `
    -Headers @{ Authorization = "Bearer $token" }

$prosConsId = $response.data[0].id
Write-Host "Testing vote for ProsConsId: $prosConsId"

# 2. 投票（upvote）
$voteBody = @{
    isUpvote = $true
} | ConvertTo-Json

$voteResponse = Invoke-RestMethod -Uri "$baseUrl/user-content/pros-cons/$prosConsId/vote" `
    -Method Post `
    -Headers @{ 
        Authorization = "Bearer $token"
        "Content-Type" = "application/json"
    } `
    -Body $voteBody

Write-Host "Vote Response: $($voteResponse | ConvertTo-Json -Depth 3)"

# 3. 再次投票（应返回 400 错误）
try {
    $duplicateVote = Invoke-RestMethod -Uri "$baseUrl/user-content/pros-cons/$prosConsId/vote" `
        -Method Post `
        -Headers @{ 
            Authorization = "Bearer $token"
            "Content-Type" = "application/json"
        } `
        -Body $voteBody
} catch {
    Write-Host "Expected error: $($_.Exception.Response.StatusCode)"
    $errorBody = $_.ErrorDetails.Message | ConvertFrom-Json
    Write-Host "Error message: $($errorBody.message)"
}

# 4. 验证 upvotes 计数增加
$updatedData = Invoke-RestMethod -Uri "$baseUrl/cities/$cityId/user-content/pros-cons" `
    -Method Get `
    -Headers @{ Authorization = "Bearer $token" }

$updatedItem = $updatedData.data | Where-Object { $_.id -eq $prosConsId }
Write-Host "Updated upvotes: $($updatedItem.upvotes)"
```

## 关键点

1. **唯一约束**: 数据库的 `uq_city_pros_cons_votes_unique` 索引保证一个用户只能为同一条目投一次票
2. **触发器维护计数**: `city_pros_cons_votes_ai` 触发器在插入投票后自动更新 `city_pros_cons` 表的 `upvotes/downvotes`
3. **错误处理**: Service 层捕获重复投票并返回友好的错误消息，Controller 转换为 HTTP 400
4. **路由分离**: 投票端点在 `/user-content/pros-cons/{id}/vote`，与城市相关的 CRUD 在 `/cities/{cityId}/user-content/pros-cons`

## Flutter 集成验证

确保 Flutter 端调用成功后:

1. 投票计数立即更新（通过触发器）
2. 二次投票返回 400 + "你已经为该条目投过票啦"
3. `ProsConsStateController.votedItemIds` 正确记录已投票 ID
4. UI 中投票按钮变为禁用/已投状态
