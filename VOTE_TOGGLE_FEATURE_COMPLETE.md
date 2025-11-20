# 投票切换功能实现完成

## 概述
成功实现了 Pros & Cons 投票切换功能，用户可以通过点击相同按钮取消投票，或点击不同按钮切换投票类型。

## 实现内容

### 1. 后端服务层逻辑 ✅
**文件**: `UserCityContentApplicationService.cs`

实现了智能投票切换逻辑：
- **相同投票类型**: 删除投票记录（取消投票）
- **不同投票类型**: 更新投票类型（从赞成变反对，或反之）
- **无投票记录**: 创建新投票记录

```csharp
public async Task VoteProsConsAsync(Guid userId, Guid prosConsId, bool isUpvote)
{
    var existingVote = await _prosConsRepository.GetUserVoteAsync(prosConsId, userId);
    
    if (existingVote != null)
    {
        if (existingVote.IsUpvote == isUpvote)
        {
            // 相同类型：取消投票
            await _prosConsRepository.DeleteVoteAsync(existingVote.Id);
            return;
        }
        else
        {
            // 不同类型：切换投票
            existingVote.IsUpvote = isUpvote;
            await _prosConsRepository.UpdateVoteAsync(existingVote);
            return;
        }
    }

    // 新投票
    await _prosConsRepository.AddVoteAsync(vote);
}
```

### 2. 仓储层实现 ✅

#### 接口 (`IUserCityProsConsRepository.cs`)
```csharp
Task<CityProsConsVote?> GetUserVoteAsync(Guid prosConsId, Guid userId);
Task<CityProsConsVote> AddVoteAsync(CityProsConsVote vote);
Task<bool> DeleteVoteAsync(Guid voteId);
Task<CityProsConsVote> UpdateVoteAsync(CityProsConsVote vote);  // ✅ 新增
```

#### 实现 (`SupabaseUserCityProsConsRepository.cs`)
```csharp
public async Task<CityProsConsVote> UpdateVoteAsync(CityProsConsVote vote)
{
    var response = await SupabaseClient
        .From<CityProsConsVote>()
        .Where(x => x.Id == vote.Id)
        .Update(vote);

    return response.Models.First();
}
```

### 3. 数据库触发器增强 ✅
**文件**: `db/pros_cons_schema.sql`

更新触发器以支持 INSERT、UPDATE 和 DELETE 操作：

```sql
CREATE OR REPLACE FUNCTION trg_city_pros_cons_vote_aggregate()
RETURNS TRIGGER AS $$
BEGIN
    IF TG_OP = 'INSERT' THEN
        -- 新增投票：增加对应计数
        UPDATE city_pros_cons SET
            upvotes   = upvotes + CASE WHEN NEW.is_upvote THEN 1 ELSE 0 END,
            downvotes = downvotes + CASE WHEN NEW.is_upvote THEN 0 ELSE 1 END,
            updated_at = now()
        WHERE id = NEW.pros_cons_id;
        RETURN NEW;
    ELSIF TG_OP = 'UPDATE' THEN
        -- 更新投票：调整计数（减去旧值，加上新值）
        UPDATE city_pros_cons SET
            upvotes   = upvotes 
                        - CASE WHEN OLD.is_upvote THEN 1 ELSE 0 END
                        + CASE WHEN NEW.is_upvote THEN 1 ELSE 0 END,
            downvotes = downvotes 
                        - CASE WHEN OLD.is_upvote THEN 0 ELSE 1 END
                        + CASE WHEN NEW.is_upvote THEN 0 ELSE 1 END,
            updated_at = now()
        WHERE id = NEW.pros_cons_id;
        RETURN NEW;
    ELSIF TG_OP = 'DELETE' THEN
        -- 删除投票：减少对应计数
        UPDATE city_pros_cons SET
            upvotes   = upvotes - CASE WHEN OLD.is_upvote THEN 1 ELSE 0 END,
            downvotes = downvotes - CASE WHEN OLD.is_upvote THEN 0 ELSE 1 END,
            updated_at = now()
        WHERE id = OLD.pros_cons_id;
        RETURN OLD;
    END IF;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER city_pros_cons_votes_ai
AFTER INSERT OR UPDATE OR DELETE ON city_pros_cons_votes  -- ✅ 支持三种操作
FOR EACH ROW EXECUTE FUNCTION trg_city_pros_cons_vote_aggregate();
```

### 4. 服务部署 ✅

- **CityService 镜像重新构建**: `city-service:latest`
- **容器重新启动**: `go-nomads-city-service`
- **Dapr Sidecar 重新启动**: `go-nomads-city-service-dapr`
- **端口映射**: 
  - CityService: `8002:8080`
  - Dapr HTTP: `3504:3504`

## 用户体验流程

### 场景 1: 初次投票
1. 用户点击 👍 或 👎 按钮
2. 系统创建新投票记录
3. 对应计数 +1

### 场景 2: 取消投票
1. 用户再次点击已投票的按钮（如再次点击 👍）
2. 系统删除投票记录
3. 对应计数 -1

### 场景 3: 切换投票类型
1. 用户点击相反的按钮（如从 👍 切换到 👎）
2. 系统更新投票记录的 `is_upvote` 字段
3. 赞成计数 -1，反对计数 +1

## API 端点
```
POST /api/v1/user-content/pros-cons/{prosConsId}/vote
```

**请求体**:
```json
{
  "isUpvote": true  // true = 赞成, false = 反对
}
```

**响应**: 204 No Content

## 数据库变更

需要在生产环境执行 SQL 更新：
```bash
# 连接到数据库并执行
psql -h <host> -U <user> -d <database> -f db/pros_cons_schema.sql
```

或者直接执行触发器更新语句（在数据库中）。

## 测试建议

### 1. 单元测试场景
- ✅ 初次投票（无现有投票）
- ✅ 取消投票（相同类型）
- ✅ 切换投票类型（不同类型）
- ✅ 投票后计数正确性

### 2. 集成测试场景
```bash
# 1. 初次投赞成票
curl -X POST http://localhost:9000/api/v1/user-content/pros-cons/{id}/vote \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{"isUpvote": true}'

# 2. 再次投赞成票（应取消）
curl -X POST http://localhost:9000/api/v1/user-content/pros-cons/{id}/vote \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{"isUpvote": true}'

# 3. 投反对票（应切换）
curl -X POST http://localhost:9000/api/v1/user-content/pros-cons/{id}/vote \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{"isUpvote": false}'
```

## Flutter 客户端考虑

当前 Flutter 实现可能需要调整：
- 已投票的按钮应有视觉反馈（如高亮显示）
- 点击已投票按钮后应清除高亮（取消投票）
- 切换投票类型时应更新两个按钮的状态

## 技术优势

1. **单次 API 调用**: 不需要分别的取消投票 API
2. **原子性操作**: 一次操作完成投票逻辑，减少并发问题
3. **触发器自动同步**: 数据库层自动维护计数准确性
4. **用户体验流畅**: 点击即生效，无需额外操作

## 完成时间
2025-01-20

## 相关文档
- [投票功能初始实现](./PROS_CONS_VOTING_FEATURE_COMPLETE.md)
- [Gateway 路由修复](./GATEWAY_ROUTING_FIX.md)
