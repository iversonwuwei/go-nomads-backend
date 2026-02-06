-- 创建用于原子递增浏览次数的函数
-- 避免读-改-写的并发问题，提升性能
CREATE OR REPLACE FUNCTION increment_innovation_view_count(p_innovation_id UUID)
RETURNS VOID AS $$
BEGIN
    UPDATE innovations 
    SET view_count = view_count + 1,
        updated_at = NOW()
    WHERE id = p_innovation_id 
      AND is_deleted IS NOT TRUE;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- 创建用于原子更新点赞计数的函数
-- delta 可以是正数（点赞）或负数（取消点赞）
CREATE OR REPLACE FUNCTION update_innovation_like_count(p_innovation_id UUID, p_delta INTEGER)
RETURNS VOID AS $$
BEGIN
    UPDATE innovations 
    SET like_count = GREATEST(0, like_count + p_delta),
        updated_at = NOW()
    WHERE id = p_innovation_id 
      AND is_deleted IS NOT TRUE;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- 创建用于原子更新评论计数的函数
-- delta 可以是正数（添加评论）或负数（删除评论）
CREATE OR REPLACE FUNCTION update_innovation_comment_count(p_innovation_id UUID, p_delta INTEGER)
RETURNS VOID AS $$
BEGIN
    UPDATE innovations 
    SET comment_count = GREATEST(0, comment_count + p_delta),
        updated_at = NOW()
    WHERE id = p_innovation_id 
      AND is_deleted IS NOT TRUE;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- 授予函数执行权限给 authenticated 和 anon 角色
GRANT EXECUTE ON FUNCTION increment_innovation_view_count(UUID) TO authenticated, anon;
GRANT EXECUTE ON FUNCTION update_innovation_like_count(UUID, INTEGER) TO authenticated;
GRANT EXECUTE ON FUNCTION update_innovation_comment_count(UUID, INTEGER) TO authenticated;

-- 添加注释
COMMENT ON FUNCTION increment_innovation_view_count IS '原子递增创新项目浏览次数，避免并发问题';
COMMENT ON FUNCTION update_innovation_like_count IS '原子更新创新项目点赞计数，delta 为正数表示点赞，负数表示取消';
COMMENT ON FUNCTION update_innovation_comment_count IS '原子更新创新项目评论计数，delta 为正数表示添加，负数表示删除';
