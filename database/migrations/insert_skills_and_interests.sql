-- ============================================
-- 技能和兴趣爱好初始化数据
-- 为数字游民平台创建预定义的技能和兴趣选项
-- ============================================

-- ============================================
-- 1. 创建技能表 (如果不存在)
-- ============================================
CREATE TABLE IF NOT EXISTS public.skills (
    id VARCHAR(50) PRIMARY KEY,
    name VARCHAR(100) NOT NULL UNIQUE,
    category VARCHAR(50) NOT NULL,
    description TEXT,
    icon VARCHAR(50),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_skills_category ON public.skills(category);

-- ============================================
-- 2. 创建兴趣爱好表 (如果不存在)
-- ============================================
CREATE TABLE IF NOT EXISTS public.interests (
    id VARCHAR(50) PRIMARY KEY,
    name VARCHAR(100) NOT NULL UNIQUE,
    category VARCHAR(50) NOT NULL,
    description TEXT,
    icon VARCHAR(50),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_interests_category ON public.interests(category);

-- ============================================
-- 3. 插入技能数据
-- ============================================

-- 编程与开发
INSERT INTO public.skills (id, name, category, description, icon) VALUES
    ('skill_javascript', 'JavaScript', 'Programming', '前端和后端开发语言', '💻'),
    ('skill_python', 'Python', 'Programming', '数据科学、AI、后端开发', '🐍'),
    ('skill_java', 'Java', 'Programming', '企业级应用开发', '☕'),
    ('skill_react', 'React', 'Programming', '前端框架', '⚛️'),
    ('skill_vue', 'Vue.js', 'Programming', '前端框架', '🟢'),
    ('skill_angular', 'Angular', 'Programming', '前端框架', '🔴'),
    ('skill_nodejs', 'Node.js', 'Programming', '后端JavaScript运行时', '🟩'),
    ('skill_golang', 'Go', 'Programming', '高性能后端开发', '🔵'),
    ('skill_rust', 'Rust', 'Programming', '系统编程语言', '🦀'),
    ('skill_flutter', 'Flutter', 'Programming', '跨平台移动开发', '📱'),
    ('skill_swift', 'Swift', 'Programming', 'iOS开发', '🍎'),
    ('skill_kotlin', 'Kotlin', 'Programming', 'Android开发', '🤖')
ON CONFLICT (name) DO NOTHING;

-- 数据与AI
INSERT INTO public.skills (id, name, category, description, icon) VALUES
    ('skill_machine_learning', 'Machine Learning', 'Data & AI', '机器学习与AI', '🤖'),
    ('skill_data_analysis', 'Data Analysis', 'Data & AI', '数据分析', '📊'),
    ('skill_sql', 'SQL', 'Data & AI', '数据库查询语言', '🗃️'),
    ('skill_data_visualization', 'Data Visualization', 'Data & AI', '数据可视化', '📈'),
    ('skill_tensorflow', 'TensorFlow', 'Data & AI', '深度学习框架', '🧠'),
    ('skill_pytorch', 'PyTorch', 'Data & AI', '深度学习框架', '🔥')
ON CONFLICT (name) DO NOTHING;

-- 设计与创意
INSERT INTO public.skills (id, name, category, description, icon) VALUES
    ('skill_ui_design', 'UI Design', 'Design', '用户界面设计', '🎨'),
    ('skill_ux_design', 'UX Design', 'Design', '用户体验设计', '✨'),
    ('skill_graphic_design', 'Graphic Design', 'Design', '平面设计', '🖼️'),
    ('skill_figma', 'Figma', 'Design', '协作设计工具', '🎭'),
    ('skill_photoshop', 'Photoshop', 'Design', '图像处理', '🖌️'),
    ('skill_illustrator', 'Illustrator', 'Design', '矢量图形设计', '✏️'),
    ('skill_video_editing', 'Video Editing', 'Design', '视频剪辑', '🎬'),
    ('skill_3d_modeling', '3D Modeling', 'Design', '三维建模', '🎲')
ON CONFLICT (name) DO NOTHING;

-- 营销与商业
INSERT INTO public.skills (id, name, category, description, icon) VALUES
    ('skill_digital_marketing', 'Digital Marketing', 'Marketing', '数字营销', '📱'),
    ('skill_seo', 'SEO', 'Marketing', '搜索引擎优化', '🔍'),
    ('skill_content_writing', 'Content Writing', 'Marketing', '内容创作', '✍️'),
    ('skill_copywriting', 'Copywriting', 'Marketing', '文案写作', '📝'),
    ('skill_social_media', 'Social Media Marketing', 'Marketing', '社交媒体营销', '📲'),
    ('skill_email_marketing', 'Email Marketing', 'Marketing', '邮件营销', '📧'),
    ('skill_analytics', 'Analytics', 'Marketing', '数据分析', '📊')
ON CONFLICT (name) DO NOTHING;

-- 项目管理
INSERT INTO public.skills (id, name, category, description, icon) VALUES
    ('skill_project_management', 'Project Management', 'Management', '项目管理', '📋'),
    ('skill_agile', 'Agile/Scrum', 'Management', '敏捷开发', '🔄'),
    ('skill_leadership', 'Leadership', 'Management', '领导力', '👥'),
    ('skill_product_management', 'Product Management', 'Management', '产品管理', '📦')
ON CONFLICT (name) DO NOTHING;

-- 语言技能
INSERT INTO public.skills (id, name, category, description, icon) VALUES
    ('skill_english', 'English', 'Languages', '英语', '🇬🇧'),
    ('skill_spanish', 'Spanish', 'Languages', '西班牙语', '🇪🇸'),
    ('skill_french', 'French', 'Languages', '法语', '🇫🇷'),
    ('skill_german', 'German', 'Languages', '德语', '🇩🇪'),
    ('skill_mandarin', 'Mandarin Chinese', 'Languages', '中文', '🇨🇳'),
    ('skill_japanese', 'Japanese', 'Languages', '日语', '🇯🇵'),
    ('skill_korean', 'Korean', 'Languages', '韩语', '🇰🇷'),
    ('skill_portuguese', 'Portuguese', 'Languages', '葡萄牙语', '🇵🇹')
ON CONFLICT (name) DO NOTHING;

-- 其他专业技能
INSERT INTO public.skills (id, name, category, description, icon) VALUES
    ('skill_photography', 'Photography', 'Creative', '摄影', '📷'),
    ('skill_blockchain', 'Blockchain', 'Technology', '区块链技术', '⛓️'),
    ('skill_cloud_computing', 'Cloud Computing', 'Technology', '云计算', '☁️'),
    ('skill_devops', 'DevOps', 'Technology', '开发运维', '🔧'),
    ('skill_cybersecurity', 'Cybersecurity', 'Technology', '网络安全', '🔒')
ON CONFLICT (name) DO NOTHING;

-- ============================================
-- 4. 插入兴趣爱好数据
-- ============================================

-- 旅行与探险
INSERT INTO public.interests (id, name, category, description, icon) VALUES
    ('interest_hiking', 'Hiking', 'Outdoor', '徒步旅行', '🥾'),
    ('interest_camping', 'Camping', 'Outdoor', '露营', '⛺'),
    ('interest_backpacking', 'Backpacking', 'Travel', '背包旅行', '🎒'),
    ('interest_photography', 'Travel Photography', 'Creative', '旅行摄影', '📸'),
    ('interest_adventure_sports', 'Adventure Sports', 'Sports', '极限运动', '🪂'),
    ('interest_scuba_diving', 'Scuba Diving', 'Water Sports', '潜水', '🤿'),
    ('interest_surfing', 'Surfing', 'Water Sports', '冲浪', '🏄'),
    ('interest_rock_climbing', 'Rock Climbing', 'Sports', '攀岩', '🧗')
ON CONFLICT (name) DO NOTHING;

-- 文化与学习
INSERT INTO public.interests (id, name, category, description, icon) VALUES
    ('interest_language_learning', 'Language Learning', 'Education', '语言学习', '📚'),
    ('interest_local_culture', 'Local Culture', 'Culture', '本地文化体验', '🏛️'),
    ('interest_cooking', 'Cooking', 'Food', '烹饪', '🍳'),
    ('interest_food_tourism', 'Food Tourism', 'Food', '美食旅游', '🍜'),
    ('interest_wine_tasting', 'Wine Tasting', 'Food', '品酒', '🍷'),
    ('interest_museums', 'Museums & Art', 'Culture', '博物馆与艺术', '🎨'),
    ('interest_reading', 'Reading', 'Education', '阅读', '📖'),
    ('interest_podcasts', 'Podcasts', 'Media', '播客', '🎙️')
ON CONFLICT (name) DO NOTHING;

-- 健康与健身
INSERT INTO public.interests (id, name, category, description, icon) VALUES
    ('interest_yoga', 'Yoga', 'Fitness', '瑜伽', '🧘'),
    ('interest_meditation', 'Meditation', 'Wellness', '冥想', '🧘‍♂️'),
    ('interest_running', 'Running', 'Fitness', '跑步', '🏃'),
    ('interest_gym', 'Gym & Fitness', 'Fitness', '健身', '💪'),
    ('interest_cycling', 'Cycling', 'Sports', '骑行', '🚴'),
    ('interest_swimming', 'Swimming', 'Sports', '游泳', '🏊'),
    ('interest_martial_arts', 'Martial Arts', 'Sports', '武术', '🥋')
ON CONFLICT (name) DO NOTHING;

-- 社交与娱乐
INSERT INTO public.interests (id, name, category, description, icon) VALUES
    ('interest_networking', 'Networking', 'Social', '社交网络', '🤝'),
    ('interest_meetups', 'Meetups & Events', 'Social', '聚会活动', '🎉'),
    ('interest_coworking', 'Coworking', 'Work', '联合办公', '💼'),
    ('interest_nightlife', 'Nightlife', 'Entertainment', '夜生活', '🌃'),
    ('interest_live_music', 'Live Music', 'Entertainment', '现场音乐', '🎵'),
    ('interest_dancing', 'Dancing', 'Entertainment', '跳舞', '💃'),
    ('interest_board_games', 'Board Games', 'Games', '桌游', '🎲'),
    ('interest_video_games', 'Video Games', 'Games', '电子游戏', '🎮')
ON CONFLICT (name) DO NOTHING;

-- 创业与科技
INSERT INTO public.interests (id, name, category, description, icon) VALUES
    ('interest_entrepreneurship', 'Entrepreneurship', 'Business', '创业', '🚀'),
    ('interest_startups', 'Startups', 'Business', '初创企业', '💡'),
    ('interest_investing', 'Investing', 'Finance', '投资', '💰'),
    ('interest_cryptocurrency', 'Cryptocurrency', 'Technology', '加密货币', '₿'),
    ('interest_tech_trends', 'Tech Trends', 'Technology', '科技趋势', '🔮'),
    ('interest_ai', 'Artificial Intelligence', 'Technology', '人工智能', '🤖'),
    ('interest_sustainability', 'Sustainability', 'Environment', '可持续发展', '🌱')
ON CONFLICT (name) DO NOTHING;

-- 艺术与音乐
INSERT INTO public.interests (id, name, category, description, icon) VALUES
    ('interest_music_production', 'Music Production', 'Creative', '音乐制作', '🎹'),
    ('interest_playing_instruments', 'Playing Instruments', 'Music', '乐器演奏', '🎸'),
    ('interest_painting', 'Painting', 'Art', '绘画', '🎨'),
    ('interest_crafts', 'Crafts & DIY', 'Creative', '手工艺', '🧵'),
    ('interest_film', 'Film & Cinema', 'Entertainment', '电影', '🎬'),
    ('interest_writing', 'Creative Writing', 'Creative', '创意写作', '✍️')
ON CONFLICT (name) DO NOTHING;

-- 自然与环境
INSERT INTO public.interests (id, name, category, description, icon) VALUES
    ('interest_wildlife', 'Wildlife & Nature', 'Nature', '野生动物与自然', '🦁'),
    ('interest_gardening', 'Gardening', 'Nature', '园艺', '🌿'),
    ('interest_bird_watching', 'Bird Watching', 'Nature', '观鸟', '🦅'),
    ('interest_eco_tourism', 'Eco-Tourism', 'Travel', '生态旅游', '🌍'),
    ('interest_volunteering', 'Volunteering', 'Community', '志愿服务', '❤️')
ON CONFLICT (name) DO NOTHING;

-- ============================================
-- 5. 创建用户技能关联表 (如果不存在)
-- ============================================
CREATE TABLE IF NOT EXISTS public.user_skills (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    user_id UUID NOT NULL,
    skill_id VARCHAR(50) NOT NULL,
    proficiency_level VARCHAR(20), -- beginner, intermediate, advanced, expert
    years_of_experience INTEGER,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE CASCADE,
    FOREIGN KEY (skill_id) REFERENCES public.skills(id) ON DELETE CASCADE,
    UNIQUE(user_id, skill_id)
);

CREATE INDEX IF NOT EXISTS idx_user_skills_user_id ON public.user_skills(user_id);
CREATE INDEX IF NOT EXISTS idx_user_skills_skill_id ON public.user_skills(skill_id);

-- ============================================
-- 6. 创建用户兴趣关联表 (如果不存在)
-- ============================================
CREATE TABLE IF NOT EXISTS public.user_interests (
    id UUID DEFAULT gen_random_uuid() PRIMARY KEY,
    user_id UUID NOT NULL,
    interest_id VARCHAR(50) NOT NULL,
    intensity_level VARCHAR(20), -- casual, moderate, passionate
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE CASCADE,
    FOREIGN KEY (interest_id) REFERENCES public.interests(id) ON DELETE CASCADE,
    UNIQUE(user_id, interest_id)
);

CREATE INDEX IF NOT EXISTS idx_user_interests_user_id ON public.user_interests(user_id);
CREATE INDEX IF NOT EXISTS idx_user_interests_interest_id ON public.user_interests(interest_id);

-- ============================================
-- 7. 启用 RLS (Row Level Security)
-- ============================================

-- 技能表 RLS
ALTER TABLE public.skills ENABLE ROW LEVEL SECURITY;

CREATE POLICY "Anyone can view skills"
ON public.skills FOR SELECT
USING (true);

-- 兴趣表 RLS
ALTER TABLE public.interests ENABLE ROW LEVEL SECURITY;

CREATE POLICY "Anyone can view interests"
ON public.interests FOR SELECT
USING (true);

-- 用户技能关联表 RLS
ALTER TABLE public.user_skills ENABLE ROW LEVEL SECURITY;

CREATE POLICY "Users can view all user skills"
ON public.user_skills FOR SELECT
USING (true);

CREATE POLICY "Users can manage their own skills"
ON public.user_skills FOR ALL
USING (true)
WITH CHECK (true);

-- 用户兴趣关联表 RLS
ALTER TABLE public.user_interests ENABLE ROW LEVEL SECURITY;

CREATE POLICY "Users can view all user interests"
ON public.user_interests FOR SELECT
USING (true);

CREATE POLICY "Users can manage their own interests"
ON public.user_interests FOR ALL
USING (true)
WITH CHECK (true);

-- ============================================
-- 8. 创建视图 - 用户完整档案
-- ============================================

CREATE OR REPLACE VIEW public.user_profiles_with_skills_interests AS
SELECT 
    u.id as user_id,
    u.name,
    u.email,
    ARRAY_AGG(DISTINCT s.name) FILTER (WHERE s.name IS NOT NULL) as skills,
    ARRAY_AGG(DISTINCT i.name) FILTER (WHERE i.name IS NOT NULL) as interests
FROM public.users u
LEFT JOIN public.user_skills us ON u.id = us.user_id
LEFT JOIN public.skills s ON us.skill_id = s.id
LEFT JOIN public.user_interests ui ON u.id = ui.user_id
LEFT JOIN public.interests i ON ui.interest_id = i.id
GROUP BY u.id, u.name, u.email;

-- ============================================
-- 完成
-- ============================================

-- 查看插入的数据统计
SELECT 
    (SELECT COUNT(*) FROM public.skills) as total_skills,
    (SELECT COUNT(*) FROM public.interests) as total_interests;

-- 按类别统计技能
SELECT category, COUNT(*) as count 
FROM public.skills 
GROUP BY category 
ORDER BY count DESC;

-- 按类别统计兴趣
SELECT category, COUNT(*) as count 
FROM public.interests 
GROUP BY category 
ORDER BY count DESC;
