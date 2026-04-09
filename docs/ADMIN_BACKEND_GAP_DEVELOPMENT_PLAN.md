# Admin Frontend API Inventory 对应 Backend 缺口开发文档

## 1. Requirement Frame

### 目标

基于 go-nomads-admin 已存在的 API 调用，补齐 go-nomads-backend 当前仍缺失的服务端能力，确保 admin 前端不再依赖空接口、不可达接口或前端代理后无上游落点的接口。

### 影响范围

- UserService
- CityService
- Gateway
- 数据库 migration（UserService / CityService）
- admin 本地代理接口的上游联通性

### 正常路径

- admin 页面调用 admin-api.ts 或本地 Next.js route
- 请求经过 Gateway 转发到对应服务
- 服务完成鉴权、参数校验、业务处理、状态落库、结构化日志输出
- 返回统一 ApiResponse 信封

### 失败路径

- 非 admin 权限：返回 403 或 401
- 参数错误：返回 400
- 资源不存在：返回 404
- 非法状态流转：返回 409 或 400
- 外部依赖失败：返回 502/500，并带结构化日志

### 回滚约束

- migration 与行为改动应分步交付
- 新增 route 不应破坏既有 route
- 对高风险动作优先保证“新增不影响旧路径”，避免替换式改造

## 2. Change Plan

### 阶段 0：保留已完成项，不重复开发

以下接口已在当前分支源码落地，不纳入本轮重复开发：

- `/api/v1/admin/city-reviews`
- `/api/v1/admin/pros-cons`
- `/api/v1/admin/moderators`
- `/api/v1/admin/moderator-applications`
- `/api/v1/admin/hotel-reviews`
- `/api/v1/admin/notifications`
- `/api/v1/admin/chats`
- `/api/v1/admin/membership/plans`
- `/api/v1/admin/travel-plans`
- `/api/v1/admin/community/posts`
- `/api/v1/admin/ai/conversations`
- `/api/v1/admin/static-texts`
- `/api/v1/admin/option-groups`
- `/api/v1/admin/config/*`

### 阶段 1：补齐 UserService 低依赖缺口

#### 1.1 Dashboard Overview

- 新增 `GET /api/v1/users/dashboard/overview`
- 建议文件层次：
  - API：新增 admin/dashboard controller 或扩展现有 Users/UserStats controller
  - Application：新增 overview query service
  - Infrastructure：复用现有 user repository / stats repository
- 最小返回字段只覆盖 admin 当前真实用量：`calculatedDate`, `users.totalUsers`, `users.newUsers`

#### 1.2 Legal List Root

- 在 LegalController 中新增 `GET /api/v1/users/legal`
- 直接从现有 legal repository 取当前版本文档列表并映射摘要 DTO

#### 1.3 Register / Forgot Password 验证码链路

- 新增 DTO：
  - `SendRegisterCodeRequest`
  - `SendForgotPasswordCodeRequest`
  - `ResetForgotPasswordRequest`
- 新增 Application 服务：
  - 生成验证码
  - 校验验证码
  - 消费验证码
  - 重置密码
- 需要 persistence：
  - 推荐新增 `verification_codes` 表
  - 或明确复用现有存储实现，但必须支持过期与幂等消费

### 阶段 2：补齐治理与审计链路

#### 2.1 Reports

- 新增 controller：`ReportsController`
- 路由：
  - `GET /api/v1/reports/my`
  - `GET /api/v1/reports/{id}`
  - `POST /api/v1/reports/{id}/{action}`
- 需要实体/仓储：
  - `Report`
  - `IReportRepository`
  - `ReportService`
- 动作状态机：
  - `pending -> assigned`
  - `pending/assigned -> resolved`
  - `pending/assigned -> dismissed`
  - 已终态禁止再次进入其他终态

#### 2.2 Admin Audit Events

- 新增 controller：`AdminAuditEventsController`
- 路由：
  - `GET /api/v1/admin/audit/events`
  - `POST /api/v1/admin/audit/events`
- 新增实体/仓储：
  - `AdminAuditEvent`
  - `IAdminAuditEventRepository`
- 同步 Gateway 路由：
  - `/api/v1/admin/audit/events`
  - `/api/v1/admin/audit/events/{**catch-all}`

#### 2.3 Gateway Reports 路由

- UserService route mapping 需要新增：
  - `/api/v1/reports`
  - `/api/v1/reports/{**catch-all}`

### 阶段 3：补齐 CityService 图片审核

#### 3.1 数据模型

- 若 `user_city_photos` 表尚无审核字段，新增 migration：
  - `moderation_status`
  - `moderation_reason`
  - `reviewed_at`
  - `reviewed_by`

#### 3.2 应用逻辑

- 在 Application 层新增照片审核 service 方法：
  - `ApprovePhotoAsync`
  - `RejectPhotoAsync`
- 拒绝时保存 reason

#### 3.3 API

- 新增 endpoints：
  - `POST /api/v1/cities/{cityId}/user-content/photos/{photoId}/approve`
  - `POST /api/v1/cities/{cityId}/user-content/photos/{photoId}/reject`
- 推荐在现有 UserCityContentController 中增加 admin-only action，减少控制器扩散

### 阶段 4：联调与契约收口

- 用 admin 前端真实调用路径做最小回归
- 校对 Next.js 本地代理与 backend 上游 path 完全一致
- 对返回 DTO 做字段名兼容校验（camelCase / PascalCase）

#### 阶段 4 当前状态

- 已完成 Gateway 鉴权收口：
  - 匿名放行 `/api/v1/auth/forgot-password*`
  - 匿名放行 `GET /api/v1/users/legal*`
  - 网关层 admin 校验覆盖 `/api/v1/admin/*` 与 `/api/v1/reports*`
- 已确认 backend 主缺口已从“接口不存在”转为“admin 展示层字段/语义对齐”
- 当前 admin 需要继续收口的真实交互项：
  - 城市图片审核：读取真实审核状态字段，不再假定首屏全为待审
  - 举报/图片审核动作：去除前端“模拟”反馈分支，按真实上游结果展示
  - 法律文档：按 `documentType` / `effectiveDate` / `isCurrent` 渲染
  - 通知：创建请求字段从 `content` 对齐为 backend `message`
  - 聊天 / AI 对话：按 backend 实体字段展示，不再沿用旧的前端会话结构假设

## 3. Validation

### 已确认现状

- 当前 backend 已完成多组 admin 主业务接口与 Gateway 管理路由补齐
- 当前 backend 已补齐本文件列出的认证恢复、治理、审计与图片审核链路
- 当前未闭环问题主要在 admin 工程对真实 DTO 的消费与展示语义

### 开发完成后最小验证

- `dotnet build go-nomads-backend.sln`
- UserService 相关测试或最小 API 验证：
  - auth send-code / reset
  - users dashboard overview
  - users legal list
  - reports action state transitions
  - admin audit events query + insert
- CityService 最小 API 验证：
  - photo approve
  - photo reject
  - photo list 对审核状态的可见性
- Gateway 最小联调：
  - `/api/v1/admin/audit/events`
  - `/api/v1/reports/{id}/assign`

### 当前未验证范围

- 邮件发送/验证码投递基础设施是否已接入真实 provider（当前验证码仍为服务端生成/记录）
- 通知、聊天、AI 会话页面在 admin 侧按真实 backend DTO 完成最终展示后的页面级交互验证

## 4. Observability

### 必加日志

- 验证码发送：scenario、target、result、failureReason
- 验证码校验：scenario、target、success、expired、consumed
- 举报动作：reportId、action、operatorId、oldStatus、newStatus
- 审计事件：scope、entityId、action、createdBy
- 图片审核：cityId、photoId、action、reviewedBy、reason

### 最小诊断点

- 对 reports、audit events、photo moderation 结果返回明确 message
- 对非法 action 与非法状态流转返回清晰错误
- 避免把“未找到资源”和“无权限操作”混成统一 500

## 5. Delivery Summary

### 已明确

- admin 主业务管理接口大部分已在当前分支实现
- 当前真正待补的 backend 缺口主要集中在 4 类：
  - Auth 验证码/找回密码
  - Dashboard overview / legal list root
  - Reports + Admin audit
  - City photo moderation

### 已规划

- 补齐对应 UserService / CityService controller、application service、repository、migration、gateway route
- 按 P0 -> P1 -> P2 分阶段交付，优先实现 admin 已直接依赖且无替代路径的接口

### 剩余风险

- 邮件验证码基础设施未确认
- 举报/审计表结构需新增 migration
- 城市图片审核可能牵涉线上数据回填

### 下一步建议

- 按本文件阶段顺序直接开始实现
- 实现顺序建议：
  1. UserService auth send-code/reset
  2. UserService dashboard overview + legal list
  3. UserService reports + audit + gateway
  4. CityService photo moderation + migration
