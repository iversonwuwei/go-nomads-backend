# Admin Frontend API Inventory 对应 Backend 缺口设计

## 1. 背景

本设计文档基于 go-nomads-admin 当前 API 盘点结果，结合 go-nomads-backend 现有源码与网关路由状态，识别仍未闭环的 backend 服务端缺口，并给出按现有微服务边界落地的设计方案。

目标不是重新设计整套后台，而是在保持现有 DDD 分层、网关转发和服务职责边界不变的前提下，补齐 admin 前端已经依赖、但 backend 仍未完整提供的接口能力。

## 2. 当前结论

### 2.1 已落地能力

以下能力在当前 backend 源码中已存在，且 admin 前端已有可对接调用：

- UserService：roles、users、users/search、users/me、membership、legal/privacy-policy
- CityService：cities、cities/list、city detail、user city photos list、admin city reviews、admin pros-cons、admin moderators、admin moderator applications
- AccommodationService：hotels、hotel detail、hotel rooms、admin hotel reviews
- EventService：events、event types
- InnovationService：innovations
- MessageService：admin notifications、admin chats
- AIService：admin travel plans、admin community posts、admin ai conversations
- ConfigService：static texts、option groups、config snapshots/publish/rollback
- Gateway：上述 admin 路由的现有补齐已在 ServiceUrlProxyConfigProvider.cs 中完成

### 2.1.1 阶段 4 联调收口结论

- P0、P1、P2 对应 backend 缺口现已完成实现并通过最小编译验证。
- Gateway 已同步放开匿名 `forgot-password` 路径与 `GET /api/v1/users/legal*`，并对 `/api/v1/admin/*` 与 `/api/v1/reports*` 施加网关层 admin 校验。
- 当前剩余问题已从“backend 缺口”收敛为“admin 工程字段映射与交互语义对齐”：
  - 城市图片审核页需要消费真实审核字段 `moderationStatus` / `reviewedAt`
  - 法律文档列表页需要按 `documentType` / `effectiveDate` / `isCurrent` 映射展示
  - 通知、聊天、AI 会话页面需要按 backend 真实实体字段做展示和创建请求适配

### 2.2 当前确认仍存在的缺口

以下表格为历史盘点结果。截止阶段 4 收口后，其中列出的 P0/P1/P2 backend 缺口已经补齐；现阶段不再作为新的 backend 开发入口，而是作为变更追溯依据保留。

以下接口在 admin 前端已存在调用，但当前 backend 源码中未找到对应完整实现，或存在明显契约缺口：

| 分类 | 接口 | 当前状态 | 建议归属服务 |
|---|---|---|---|
| Auth 验证码 | POST /api/v1/auth/register/send-code | 缺失 | UserService |
| Auth 找回密码 | POST /api/v1/auth/forgot-password/send-code | 缺失 | UserService |
| Auth 找回密码 | POST /api/v1/auth/forgot-password/reset | 缺失 | UserService |
| Dashboard 聚合 | GET /api/v1/users/dashboard/overview | 缺失 | UserService |
| 举报列表 | GET /api/v1/reports/my | 缺失 | UserService |
| 举报详情 | GET /api/v1/reports/{id} | 缺失 | UserService |
| 举报处置 | POST /api/v1/reports/{id}/{action} | 缺失 | UserService |
| Admin 审计 | GET /api/v1/admin/audit/events | 缺失 | UserService |
| Admin 审计 | POST /api/v1/admin/audit/events | 缺失 | UserService |
| 法律文档列表 | GET /api/v1/users/legal | 缺失根路由，仅有 privacy-policy / terms-of-service / history | UserService |
| 城市图片审核 | POST /api/v1/cities/{cityId}/user-content/photos/{photoId}/approve | 缺失 | CityService |
| 城市图片审核 | POST /api/v1/cities/{cityId}/user-content/photos/{photoId}/reject | 缺失 | CityService |

## 3. 设计原则

- 保持现有服务边界，不为 admin 补接口单独引入新微服务。
- API 层只做鉴权、参数校验和返回封装；业务逻辑下沉到 Application；数据落库在 Infrastructure。
- 缺口优先做“最小闭环”实现：接口、状态落库、结构化日志、网关路由、最小验证一起交付。
- 对 admin 可感知的动作接口，返回明确成功/失败语义，避免统一 500。
- 新增 `/api/v1/admin/*` 路由时，必须同步 Gateway 路由映射，否则 admin 前端虽可编译但无法联通。

## 4. 分模块设计

### 4.1 UserService：认证验证码与找回密码

#### 目标接口

- POST `/api/v1/auth/register/send-code`
- POST `/api/v1/auth/forgot-password/send-code`
- POST `/api/v1/auth/forgot-password/reset`

#### 设计方案

- 在 UserService Application 层新增验证码场景服务，统一处理 `register` 与 `forgot-password` 两类验证码。
- 验证码数据建议显式持久化，避免仅保存在内存中导致多实例/重启丢失。
- 建议数据结构至少包含：
  - `Id`
  - `Target`（email 或 phone）
  - `Scenario`（register / forgot_password）
  - `CodeHash`
  - `ExpiresAt`
  - `ConsumedAt`
  - `AttemptCount`
  - `CreatedAt`
- `reset` 接口职责：校验 target + code + 未过期 + 未消费，成功后更新密码哈希，并使验证码失效。

#### 兼容性要求

- 不破坏现有 `POST /api/v1/auth/register` 与 `POST /api/v1/auth/login`。
- register 端点后续可以选择验证 `verificationCode`，但首阶段应保持向后兼容，允许 feature flag 或配置控制“是否强制验证码”。

#### 观测要求

- 记录场景、目标、发送结果、失败原因、是否过期、是否重复消费。
- 日志中不得输出明文验证码。

### 4.2 UserService：Dashboard Overview 聚合

#### 目标接口

- GET `/api/v1/users/dashboard/overview`

#### 设计方案

- 在 UserService API/Application 层增加 admin overview 查询入口。
- 首阶段只返回 admin 前端当前真实使用字段：
  - `calculatedDate`
  - `users.totalUsers`
  - `users.newUsers`
- `cities/coworkings/meetups/innovations` 当前由 admin 前端分别调用其他服务统计总数，无需在此接口里重复聚合。
- `newUsers` 建议定义为最近 30 天新增用户数，查询口径在文档中固定。

#### 返回契约

```json
{
  "success": true,
  "message": "OK",
  "data": {
    "calculatedDate": "2026-04-08T12:00:00Z",
    "users": {
      "totalUsers": 1024,
      "newUsers": 87
    }
  },
  "errors": []
}
```

### 4.3 UserService：举报中心

#### 目标接口

- GET `/api/v1/reports/my`
- GET `/api/v1/reports/{id}`
- POST `/api/v1/reports/{id}/{action}`，其中 `action in {assign, resolve, dismiss}`

#### 设计方案

- 举报属于平台治理与 admin 操作汇总，继续放在 UserService，避免为 admin 后台拆散到多个业务服务。
- Domain 建议新增：
  - `Report`
  - `ReportActionRecord`（可选，若需要动作历史）
- `Report` 建议字段：
  - `Id`
  - `ReporterId`
  - `ReporterNameSnapshot`
  - `ContentType`
  - `TargetId`
  - `TargetNameSnapshot`
  - `ReasonId`
  - `ReasonLabel`
  - `Status`（pending / assigned / resolved / dismissed）
  - `AdminNotes`
  - `AssignedAdminId`
  - `ResolvedAt`
  - `DismissedAt`
  - `CreatedAt`
  - `UpdatedAt`
- `POST /reports/{id}/{action}` 应做状态机校验，避免重复 resolve 或 dismiss。

#### 动作语义

- `assign`：写入管理员、更新时间、备注
- `resolve`：写入终态、备注、操作时间
- `dismiss`：写入驳回终态、备注、操作时间

#### 鉴权

- `my` 与 detail 可先按 admin-only 处理，保持和当前 admin 页面一致。
- 若未来 App 端也复用举报中心，再拆分 user-facing 与 admin-facing 视角。

### 4.4 UserService：Admin Audit Events

#### 目标接口

- GET `/api/v1/admin/audit/events?scope=...`
- POST `/api/v1/admin/audit/events`

#### 设计方案

- 审计事件属于后台治理元数据，继续放在 UserService。
- 新增实体 `AdminAuditEvent`，建议字段：
  - `Id`
  - `Scope`
  - `EntityId`
  - `Action`
  - `Note`
  - `MetadataJson`
  - `HappenedAt`
  - `CreatedBy`
  - `CreatedAt`
- GET 默认按 `scope` 过滤，并按 `happenedAt desc` 返回。
- POST 由前端传来的 `metadata` 保留为 JSON，后端只做大小和结构保护，不做强 schema 约束。

#### 网关要求

- UserService 需要新增 `/api/v1/admin/audit/events` 与 catch-all 路由映射。

### 4.5 UserService：法律文档列表根路由

#### 目标接口

- GET `/api/v1/users/legal`

#### 设计方案

- 复用现有 LegalController 与 ILegalDocumentRepository。
- 返回当前版本的法律文档摘要列表，至少包含：
  - `id`
  - `slug` 或 `documentType`
  - `title`
  - `language`
  - `version`
  - `status`
  - `publishedAt`
  - `updatedAt`
- `status` 建议按 `isCurrent` 映射为 `published` / `archived`。

### 4.6 CityService：城市图片审核

#### 目标接口

- POST `/api/v1/cities/{cityId}/user-content/photos/{photoId}/approve`
- POST `/api/v1/cities/{cityId}/user-content/photos/{photoId}/reject`

#### 设计方案

- 保持在现有 UserCityContentController 或单独新增 Admin/Moderation controller。
- 审核动作应为 admin-only。
- 照片实体或对应表建议增加字段：
  - `ModerationStatus`（pending / approved / rejected）
  - `ModerationReason`
  - `ReviewedAt`
  - `ReviewedBy`
- `approve`/`reject` 只改审核状态，不删除原数据。
- 后续 GET `/cities/{cityId}/user-content/photos` 默认对普通用户只返回 `approved`；admin 视图可看全部状态。

#### 回滚面

- 若数据库尚未有审核字段，需要单独 migration。
- migration 与 controller 上线可拆两步，避免接口先上线但状态无法持久化。

## 5. 建议交付顺序

### P0 必做

- UserService：`/auth/register/send-code`
- UserService：`/auth/forgot-password/send-code`
- UserService：`/auth/forgot-password/reset`
- UserService：`/users/dashboard/overview`
- UserService：`/users/legal`

### P1 必做

- UserService：`/reports/my`
- UserService：`/reports/{id}`
- UserService：`/reports/{id}/{action}`
- UserService：`/admin/audit/events`
- Gateway：`/api/v1/reports*`、`/api/v1/admin/audit/events*`

### P2 必做

- CityService：图片审核 approve/reject
- CityService：必要的审核字段 migration

## 6. 风险与未决事项

- 当前仓库中未确认已有通用“邮件验证码发送”基础设施，验证码发送能力可能需要新增基础服务或复用现有短信/通知能力。
- 举报与审计表结构当前源码中未找到现成实现，需补 migration 与 repository。
- admin 前端对 report/audit/city-photo review 已有调用，backend 未补齐前应继续视为不可上线能力，而不是 UI 可用能力。
- Travel plan detail、analytics 页仍有前端占位，但这属于前端 UI/字段增强，不属于当前 backend 缺口的第一优先级。
