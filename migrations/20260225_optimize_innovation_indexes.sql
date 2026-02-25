-- ============================================================================
-- 创新项目表性能优化 - 复合索引 + 条件索引 + 全文搜索
-- Migration: 20260225_optimize_innovation_indexes.sql
-- 
-- 背景：当前 innovations 相关表仅有单列索引，而实际查询全是多列过滤+排序组合，
--       导致 PostgreSQL 无法有效利用索引，数据量增长后必然退化为顺序扫描。
--
-- 优化策略：
--   1. 用 Partial Index + 复合列替代单列索引，覆盖所有核心查询路径
--   2. 添加 GIN trigram 索引支持 ILIKE 模糊搜索
--   3. 创建列表视图减少网络传输（可选）
--   4. 创建批量点赞状态查询函数，把 N+1 推到数据库侧完成
--
-- 注意：如果是在生产环境大表上执行，建议将 CREATE INDEX 改为
--       CREATE INDEX CONCURRENTLY 并逐条在事务外执行，避免锁表。
-- ============================================================================

BEGIN;

-- ============================================================================
-- 1. innovations 表 - 复合索引（替代低效的单列索引）
-- ============================================================================

-- 1.1 列表查询主索引（GetAllAsync 核心路径）
-- 查询模式: WHERE is_public = true AND is_deleted = false [AND category = ?] ORDER BY created_at DESC
CREATE INDEX IF NOT EXISTS idx_innovations_list_main
    ON innovations (is_public, category, created_at DESC)
    WHERE is_deleted = false;

-- 1.2 精选项目索引（GetFeaturedAsync）
-- 查询模式: WHERE is_featured = true AND is_public = true AND is_deleted = false ORDER BY created_at DESC
CREATE INDEX IF NOT EXISTS idx_innovations_featured
    ON innovations (created_at DESC)
    WHERE is_featured = true AND is_public = true AND is_deleted = false;

-- 1.3 热门项目索引（GetPopularAsync）
-- 查询模式: WHERE is_public = true AND is_deleted = false ORDER BY like_count DESC
CREATE INDEX IF NOT EXISTS idx_innovations_popular
    ON innovations (like_count DESC)
    WHERE is_public = true AND is_deleted = false;

-- 1.4 用户项目索引（GetByUserIdAsync）
-- 查询模式: WHERE creator_id = ? AND is_deleted = false ORDER BY created_at DESC
CREATE INDEX IF NOT EXISTS idx_innovations_by_creator
    ON innovations (creator_id, created_at DESC)
    WHERE is_deleted = false;

-- 1.5 标题模糊搜索 GIN trigram 索引（GetAllAsync search 参数）
CREATE EXTENSION IF NOT EXISTS pg_trgm;

CREATE INDEX IF NOT EXISTS idx_innovations_title_trgm
    ON innovations USING gin (title gin_trgm_ops)
    WHERE is_deleted = false;

-- 1.6 阶段(stage)筛选复合索引
CREATE INDEX IF NOT EXISTS idx_innovations_by_stage
    ON innovations (stage, created_at DESC)
    WHERE is_public = true AND is_deleted = false;

-- ============================================================================
-- 2. innovation_comments 表 - 复合索引
-- ============================================================================

-- 2.1 评论分页查询索引（GetCommentsAsync）
-- 查询模式: WHERE innovation_id = ? ORDER BY created_at DESC LIMIT x OFFSET y
CREATE INDEX IF NOT EXISTS idx_innovation_comments_paged
    ON innovation_comments (innovation_id, created_at DESC);

-- ============================================================================
-- 3. innovation_team_members 表 - 复合排序索引
-- ============================================================================

-- 3.1 团队成员查询索引（GetTeamMembersAsync）
-- 查询模式: WHERE innovation_id = ? ORDER BY is_founder DESC, joined_at ASC
CREATE INDEX IF NOT EXISTS idx_innovation_team_sorted
    ON innovation_team_members (innovation_id, is_founder DESC, joined_at ASC);

-- ============================================================================
-- 4. 清理可被新复合索引覆盖的旧单列索引
--    注意：保守策略，只删除明确被新索引完全覆盖的旧索引
-- ============================================================================

-- idx_innovations_is_deleted: 被所有 Partial Index 的 WHERE is_deleted = false 条件覆盖
DROP INDEX IF EXISTS idx_innovations_is_deleted;

-- idx_innovations_is_featured: 被 idx_innovations_featured (Partial Index) 完全覆盖
DROP INDEX IF EXISTS idx_innovations_is_featured;

-- idx_innovations_like_count: 被 idx_innovations_popular (Partial Index + like_count DESC) 覆盖
DROP INDEX IF EXISTS idx_innovations_like_count;

-- idx_innovation_comments_innovation_id: 被 idx_innovation_comments_paged 覆盖（前导列相同）
DROP INDEX IF EXISTS idx_innovation_comments_innovation_id;

-- ============================================================================
-- 5. 列表视图 - 只包含列表页需要的列，减少网络 I/O
--    应用层可直接查询此视图代替 innovations 表
-- ============================================================================

CREATE OR REPLACE VIEW v_innovation_list AS
SELECT
    id,
    title,
    elevator_pitch,
    product_type,
    key_features,
    category,
    stage,
    image_url,
    creator_id,
    creator_name,
    creator_avatar,
    team_size,
    like_count,
    view_count,
    comment_count,
    is_featured,
    is_public,
    is_deleted,
    created_at
FROM innovations;

-- ============================================================================
-- 6. 批量获取点赞状态的数据库函数
--    将原来的 N 次查询合并为 1 次，在数据库侧完成集合运算
-- ============================================================================

CREATE OR REPLACE FUNCTION get_liked_innovation_ids(
    p_user_id UUID,
    p_innovation_ids UUID[]
)
RETURNS TABLE(innovation_id UUID) AS $$
BEGIN
    RETURN QUERY
    SELECT il.innovation_id
    FROM innovation_likes il
    WHERE il.user_id = p_user_id
      AND il.innovation_id = ANY(p_innovation_ids);
END;
$$ LANGUAGE plpgsql STABLE;

-- ============================================================================
-- 7. 更新表的统计信息，让优化器能立即使用新索引
-- ============================================================================

ANALYZE innovations;
ANALYZE innovation_likes;
ANALYZE innovation_comments;
ANALYZE innovation_team_members;

COMMIT;

-- ============================================================================
-- 验证索引创建情况（执行后手动检查）
-- ============================================================================
-- SELECT indexname, indexdef 
-- FROM pg_indexes 
-- WHERE tablename IN ('innovations', 'innovation_likes', 'innovation_comments', 'innovation_team_members')
-- ORDER BY tablename, indexname;
