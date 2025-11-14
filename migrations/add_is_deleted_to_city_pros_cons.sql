-- 为 city_pros_cons 表添加 is_deleted 列用于逻辑删除
ALTER TABLE city_pros_cons
ADD COLUMN IF NOT EXISTS is_deleted BOOLEAN NOT NULL DEFAULT false;

-- 创建索引以提高查询性能
CREATE INDEX IF NOT EXISTS idx_city_pros_cons_is_deleted ON city_pros_cons(is_deleted);

-- 为查询优化创建复合索引
CREATE INDEX IF NOT EXISTS idx_city_pros_cons_city_deleted ON city_pros_cons(city_id, is_deleted);

-- 注释说明
COMMENT ON COLUMN city_pros_cons.is_deleted IS '逻辑删除标记，true表示已删除';
