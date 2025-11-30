-- =============================================================
-- 确保 coworking_verifications 表的唯一约束存在
-- 防止单一用户对同一个 coworking space 提交多次验证
-- =============================================================

-- 1. 清理历史重复数据（保留最早的记录）
WITH ranked_verifications AS (
    SELECT
        id,
        ROW_NUMBER() OVER (PARTITION BY coworking_id, user_id ORDER BY created_at ASC) AS rn
    FROM public.coworking_verifications
)
DELETE FROM public.coworking_verifications
WHERE id IN (
    SELECT id
    FROM ranked_verifications
    WHERE rn > 1
);

-- 2. 删除旧的可能存在的索引
DROP INDEX IF EXISTS public.idx_coworking_verifications_unique;
DROP INDEX IF EXISTS public.idx_coworking_verifications_unique_vote;

-- 3. 创建唯一约束（确保只有一个唯一约束）
CREATE UNIQUE INDEX idx_coworking_verifications_unique_vote
    ON public.coworking_verifications (coworking_id, user_id);

-- 4. 添加注释
COMMENT ON INDEX public.idx_coworking_verifications_unique_vote IS '确保每个用户对每个 coworking space 只能验证一次';
