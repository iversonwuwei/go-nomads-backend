-- 创建城市版主关联表（多对多）
-- 一个城市可以有多个版主，一个用户可以是多个城市的版主

-- 删除旧的版主列（如果存在）
-- ALTER TABLE cities DROP COLUMN IF EXISTS moderator_id;

-- 创建城市版主关联表
CREATE TABLE IF NOT EXISTS city_moderators (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    city_id UUID NOT NULL REFERENCES cities(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    
    -- 版主权限范围（可选，用于细粒度权限控制）
    can_edit_city BOOLEAN DEFAULT TRUE,
    can_manage_coworks BOOLEAN DEFAULT TRUE,
    can_manage_costs BOOLEAN DEFAULT TRUE,
    can_manage_visas BOOLEAN DEFAULT TRUE,
    can_moderate_chats BOOLEAN DEFAULT TRUE,
    
    -- 指定信息
    assigned_by UUID REFERENCES users(id), -- 谁指定的这个版主
    assigned_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    
    -- 版主状态
    is_active BOOLEAN DEFAULT TRUE,
    notes TEXT, -- 备注信息
    
    -- 时间戳
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    
    -- 唯一约束：同一个用户不能重复成为同一个城市的版主
    UNIQUE(city_id, user_id)
);

-- 创建索引
CREATE INDEX IF NOT EXISTS idx_city_moderators_city_id ON city_moderators(city_id);
CREATE INDEX IF NOT EXISTS idx_city_moderators_user_id ON city_moderators(user_id);
CREATE INDEX IF NOT EXISTS idx_city_moderators_is_active ON city_moderators(is_active);

-- 添加注释
COMMENT ON TABLE city_moderators IS '城市版主关联表 - 支持一个城市多个版主，一个用户管理多个城市';
COMMENT ON COLUMN city_moderators.city_id IS '城市ID';
COMMENT ON COLUMN city_moderators.user_id IS '版主用户ID';
COMMENT ON COLUMN city_moderators.can_edit_city IS '是否可以编辑城市基本信息';
COMMENT ON COLUMN city_moderators.can_manage_coworks IS '是否可以管理联合办公空间';
COMMENT ON COLUMN city_moderators.can_manage_costs IS '是否可以管理生活成本信息';
COMMENT ON COLUMN city_moderators.can_manage_visas IS '是否可以管理签证信息';
COMMENT ON COLUMN city_moderators.can_moderate_chats IS '是否可以管理城市聊天室';
COMMENT ON COLUMN city_moderators.assigned_by IS '指定该版主的管理员用户ID';
COMMENT ON COLUMN city_moderators.assigned_at IS '指定时间';
COMMENT ON COLUMN city_moderators.is_active IS '版主是否激活';
COMMENT ON COLUMN city_moderators.notes IS '备注信息，如指定原因等';

-- 迁移现有数据（如果 cities 表中有 moderator_id）
-- INSERT INTO city_moderators (city_id, user_id, assigned_at)
-- SELECT id, moderator_id, updated_at
-- FROM cities
-- WHERE moderator_id IS NOT NULL
-- ON CONFLICT (city_id, user_id) DO NOTHING;
