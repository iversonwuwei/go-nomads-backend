-- 插入团队成员测试数据
-- Migration: 20241226_insert_team_members_test_data.sql
-- 依赖: 20241226_insert_innovation_test_data.sql

-- 项目1: 智课通 团队成员
INSERT INTO public.innovation_team_members (innovation_id, name, role, description, is_founder) VALUES
('a1b2c3d4-e5f6-7890-abcd-ef1234567001', '张三', 'CEO', '前腾讯产品经理，5年互联网经验', true),
('a1b2c3d4-e5f6-7890-abcd-ef1234567001', '李四', 'CTO', '计算机硕士，擅长AI算法开发', true);

-- 项目2: 碳足迹追踪器 团队成员
INSERT INTO public.innovation_team_members (innovation_id, name, role, description, is_founder) VALUES
('a1b2c3d4-e5f6-7890-abcd-ef1234567002', '王五', 'CEO', '环保行业10年经验，前WWF项目负责人', true),
('a1b2c3d4-e5f6-7890-abcd-ef1234567002', '赵六', 'CTO', '前阿里云高级工程师，专注大数据分析', true),
('a1b2c3d4-e5f6-7890-abcd-ef1234567002', '孙七', '产品经理', '3年C端产品经验，用户增长专家', false);

-- 项目3: 远程协作白板 团队成员
INSERT INTO public.innovation_team_members (innovation_id, name, role, description, is_founder) VALUES
('a1b2c3d4-e5f6-7890-abcd-ef1234567003', '周八', '创始人/全栈开发', '独立开发者，曾开发过多款工具类产品', true);
