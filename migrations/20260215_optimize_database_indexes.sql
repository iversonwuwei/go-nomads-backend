-- ============================================================
-- 数据库索引全面优化迁移
-- 日期: 2026-02-15
-- 说明: 基于仓储层查询模式分析，添加缺失索引并优化现有索引
-- ============================================================

-- ============================================================
-- 0. 启用 pg_trgm 扩展（模糊搜索 ILIKE 加速）
-- ============================================================
CREATE EXTENSION IF NOT EXISTS pg_trgm;

-- ============================================================
-- 1. cities 表 — 城市搜索和列表是核心高频查询
-- ============================================================

-- 城市名称模糊搜索（ILIKE），核心搜索功能
CREATE INDEX IF NOT EXISTS idx_cities_name_trgm
    ON cities USING GIN(name gin_trgm_ops);

-- 英文名模糊搜索
CREATE INDEX IF NOT EXISTS idx_cities_name_en_trgm
    ON cities USING GIN(name_en gin_trgm_ops);

-- 按区域筛选
CREATE INDEX IF NOT EXISTS idx_cities_region
    ON cities(region);

-- 城市列表页核心查询：活跃+未删除+评分排序
CREATE INDEX IF NOT EXISTS idx_cities_active_score
    ON cities(overall_score DESC)
    WHERE is_active = true AND is_deleted = false;

-- ============================================================
-- 2. users 表 — 用户名搜索
-- ============================================================

-- 用户名模糊搜索
CREATE INDEX IF NOT EXISTS idx_users_name_trgm
    ON users USING GIN(name gin_trgm_ops);

-- ============================================================
-- 3. orders 表 — 订单查询优化
-- ============================================================

-- 用户订单分页查询（复合索引替代分离的 user_id + created_at 索引）
CREATE INDEX IF NOT EXISTS idx_orders_user_created
    ON orders(user_id, created_at DESC);

-- 过期待支付订单扫描（定时任务，部分索引）
CREATE INDEX IF NOT EXISTS idx_orders_pending_expired
    ON orders(expired_at)
    WHERE status = 'pending';

-- ============================================================
-- 4. memberships 表 — 会员到期查询优化
-- ============================================================

-- 到期/续费定时任务查询（level > 0 的有效会员按到期日排序）
CREATE INDEX IF NOT EXISTS idx_memberships_level_expiry
    ON memberships(level, expiry_date)
    WHERE level > 0;

-- ============================================================
-- 5. travel_history 表 — 旅行记录分页优化
-- ============================================================

-- 用户旅行记录分页（高频查询）
CREATE INDEX IF NOT EXISTS idx_travel_history_user_arrival
    ON travel_history(user_id, arrival_time DESC);

-- 带确认状态过滤的分页查询
CREATE INDEX IF NOT EXISTS idx_travel_history_user_confirmed
    ON travel_history(user_id, is_confirmed, arrival_time DESC);

-- 用户+城市联合查询
CREATE INDEX IF NOT EXISTS idx_travel_history_user_city
    ON travel_history(user_id, city_id);

-- ============================================================
-- 6. visited_places 表 — 足迹查询优化
-- ============================================================

-- 按旅行记录+时间排序（详情页展示足迹轨迹）
CREATE INDEX IF NOT EXISTS idx_visited_places_th_arrival
    ON visited_places(travel_history_id, arrival_time ASC);

-- 按用户+时间排序（用户所有足迹列表）
CREATE INDEX IF NOT EXISTS idx_visited_places_user_arrival
    ON visited_places(user_id, arrival_time DESC);

-- ============================================================
-- 7. events 表 — 活动列表查询优化
-- ============================================================

-- 活动列表页核心复合索引（按城市+状态+时间排序）
CREATE INDEX IF NOT EXISTS idx_events_active_list
    ON events(city_id, status, start_time ASC)
    WHERE is_deleted = false;

-- 过期活动检查（定时任务）
CREATE INDEX IF NOT EXISTS idx_events_upcoming_start
    ON events(start_time)
    WHERE status = 'upcoming' AND is_deleted = false;

-- ============================================================
-- 8. event_invitations 表 — 邀请查询优化
-- ============================================================

-- 邀请人+状态复合查询
CREATE INDEX IF NOT EXISTS idx_event_invitations_inviter_status
    ON event_invitations(inviter_id, status);

-- ============================================================
-- 9. coworking_spaces 表 — 共享空间查询优化
-- ============================================================

-- 按城市的活跃空间列表（核心查询：城市内按评分排序）
CREATE INDEX IF NOT EXISTS idx_coworking_city_active_rating
    ON coworking_spaces(city_id, rating DESC)
    WHERE is_active = true AND is_deleted = false;

-- Top rated 全局查询
CREATE INDEX IF NOT EXISTS idx_coworking_active_rating
    ON coworking_spaces(rating DESC)
    WHERE is_active = true AND is_deleted = false;

-- 空间名称模糊搜索
CREATE INDEX IF NOT EXISTS idx_coworking_name_trgm
    ON coworking_spaces USING GIN(name gin_trgm_ops);

-- 创建者信息更新查询
CREATE INDEX IF NOT EXISTS idx_coworking_created_by
    ON coworking_spaces(created_by)
    WHERE is_deleted = false;

-- ============================================================
-- 10. coworking_bookings 表 — 预订查询优化
-- ============================================================

-- 按空间的预订列表
CREATE INDEX IF NOT EXISTS idx_coworking_bookings_coworking
    ON coworking_bookings(coworking_id, booking_date DESC);

-- 预订冲突检测（每次预订时调用）
CREATE INDEX IF NOT EXISTS idx_coworking_bookings_conflict
    ON coworking_bookings(coworking_id, booking_date)
    WHERE status != 'cancelled';

-- 用户预订+状态过滤
CREATE INDEX IF NOT EXISTS idx_coworking_bookings_user_status
    ON coworking_bookings(user_id, status, booking_date DESC);

-- ============================================================
-- 11. user_city_reviews 表 — 评论查询优化
-- ============================================================

-- 城市+用户联合查询（查看某用户对某城市的评论）
CREATE INDEX IF NOT EXISTS idx_user_city_reviews_city_user
    ON user_city_reviews(city_id, user_id);

-- ============================================================
-- 12. chat_rooms 表 — 聊天房间查询优化
-- ============================================================

-- 私聊房间按 name 精确查找（高频操作：检查私聊是否已存在）
CREATE INDEX IF NOT EXISTS idx_chat_rooms_direct_name
    ON chat_rooms(name)
    WHERE room_type = 'direct' AND is_deleted = false;

-- ============================================================
-- 13. chat_room_messages 表 — 消息搜索
-- ============================================================

-- 聊天消息内容模糊搜索
CREATE INDEX IF NOT EXISTS idx_chat_room_messages_trgm
    ON chat_room_messages USING GIN(message gin_trgm_ops);
