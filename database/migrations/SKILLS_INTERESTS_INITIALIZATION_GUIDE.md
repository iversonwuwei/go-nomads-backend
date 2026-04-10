# 技能和兴趣爱好数据初始化指南

## 📋 概述

本脚本为数字游民平台初始化技能和兴趣爱好数据，包括:
- **54 个技能** (分为编程、设计、营销、管理、语言等类别)
- **50+ 个兴趣爱好** (分为旅行、文化、健身、社交等类别)

## 🗂️ 创建的表结构

### 1. `skills` - 技能表
| 字段 | 类型 | 说明 |
|------|------|------|
| id | VARCHAR(50) | 主键 |
| name | VARCHAR(100) | 技能名称 |
| category | VARCHAR(50) | 类别 |
| description | TEXT | 描述 |
| icon | VARCHAR(50) | 图标 |
| created_at | TIMESTAMP | 创建时间 |

### 2. `interests` - 兴趣表
| 字段 | 类型 | 说明 |
|------|------|------|
| id | VARCHAR(50) | 主键 |
| name | VARCHAR(100) | 兴趣名称 |
| category | VARCHAR(50) | 类别 |
| description | TEXT | 描述 |
| icon | VARCHAR(50) | 图标 |
| created_at | TIMESTAMP | 创建时间 |

### 3. `user_skills` - 用户技能关联表
| 字段 | 类型 | 说明 |
|------|------|------|
| id | UUID | 主键 |
| user_id | VARCHAR(50) | 用户ID (外键) |
| skill_id | VARCHAR(50) | 技能ID (外键) |
| proficiency_level | VARCHAR(20) | 熟练度 (beginner/intermediate/advanced/expert) |
| years_of_experience | INTEGER | 经验年限 |
| created_at | TIMESTAMP | 创建时间 |

### 4. `user_interests` - 用户兴趣关联表
| 字段 | 类型 | 说明 |
|------|------|------|
| id | UUID | 主键 |
| user_id | VARCHAR(50) | 用户ID (外键) |
| interest_id | VARCHAR(50) | 兴趣ID (外键) |
| intensity_level | VARCHAR(20) | 强度 (casual/moderate/passionate) |
| created_at | TIMESTAMP | 创建时间 |

## 🚀 执行步骤

### 方法 1: Supabase Dashboard (推荐)

1. **访问 Supabase SQL Editor**
   ```
   https://supabase.com/dashboard/project/lcfbajrocmjlqndkrsao/sql/new
   ```

2. **复制 SQL 脚本**
   - 打开文件: `database/migrations/insert_skills_and_interests.sql`
   - 复制全部内容

3. **执行脚本**
   - 粘贴到 SQL Editor
   - 点击 **Run** 按钮 (或按 Cmd/Ctrl + Enter)

4. **验证执行结果**
   - 查看返回的统计信息
   - 应该显示技能和兴趣的总数

### 方法 2: 使用 psql 命令行

```bash
# 设置连接信息
export PGHOST="db.lcfbajrocmjlqndkrsao.supabase.co"
export PGPORT="6543"
export PGDATABASE="postgres"
export PGUSER="postgres"
export PGPASSWORD="bwTyaM1eJ1TRIZI3"

# 执行脚本
psql -f database/migrations/insert_skills_and_interests.sql
```

### 方法 3: 使用数据库工具 (DBeaver, TablePlus, pgAdmin)

1. 连接到 Supabase 数据库
   - Host: `db.lcfbajrocmjlqndkrsao.supabase.co`
   - Port: `6543`
   - Database: `postgres`
   - User: `postgres`
   - Password: `bwTyaM1eJ1TRIZI3`

2. 打开 SQL 文件 `insert_skills_and_interests.sql`
3. 执行脚本

## 📊 数据分类

### 技能类别

| 类别 | 数量 | 示例 |
|------|------|------|
| Programming | 12 | JavaScript, Python, React, Flutter |
| Data & AI | 6 | Machine Learning, SQL, TensorFlow |
| Design | 8 | UI Design, Figma, Photoshop |
| Marketing | 7 | SEO, Content Writing, Social Media |
| Management | 4 | Project Management, Agile, Leadership |
| Languages | 8 | English, Spanish, Mandarin |
| Technology | 5 | Cloud Computing, Blockchain, DevOps |
| Creative | 1 | Photography |

**总计**: ~51 个技能

### 兴趣类别

| 类别 | 示例 |
|------|------|
| Outdoor | Hiking, Camping |
| Travel | Backpacking, Eco-Tourism |
| Sports | Surfing, Rock Climbing, Cycling |
| Culture | Local Culture, Museums, Cooking |
| Fitness | Yoga, Running, Gym |
| Social | Networking, Meetups, Coworking |
| Business | Entrepreneurship, Startups, Investing |
| Creative | Music Production, Painting, Writing |
| Nature | Wildlife, Gardening, Bird Watching |
| Technology | AI, Cryptocurrency, Tech Trends |

**总计**: ~50 个兴趣

## 🔍 验证脚本

执行完成后，运行以下查询验证:

```sql
-- 查看技能总数
SELECT COUNT(*) FROM public.skills;

-- 按类别统计技能
SELECT category, COUNT(*) as count 
FROM public.skills 
GROUP BY category 
ORDER BY count DESC;

-- 查看所有技能
SELECT id, name, category, icon 
FROM public.skills 
ORDER BY category, name;

-- 查看兴趣总数
SELECT COUNT(*) FROM public.interests;

-- 按类别统计兴趣
SELECT category, COUNT(*) as count 
FROM public.interests 
GROUP BY category 
ORDER BY count DESC;

-- 查看所有兴趣
SELECT id, name, category, icon 
FROM public.interests 
ORDER BY category, name;
```

## 📱 Flutter 前端使用示例

### 获取所有技能

```dart
Future<List<Skill>> getAllSkills() async {
  final response = await httpService.get('/skills');
  return (response.data as List)
      .map((json) => Skill.fromJson(json))
      .toList();
}
```

### 按类别获取技能

```dart
Future<List<Skill>> getSkillsByCategory(String category) async {
  final response = await httpService.get('/skills?category=$category');
  return (response.data as List)
      .map((json) => Skill.fromJson(json))
      .toList();
}
```

### 添加用户技能

```dart
Future<void> addUserSkill(String userId, String skillId, {
  String? proficiencyLevel,
  int? yearsOfExperience,
}) async {
  await httpService.post('/users/$userId/skills', data: {
    'skill_id': skillId,
    'proficiency_level': proficiencyLevel,
    'years_of_experience': yearsOfExperience,
  });
}
```

### 获取用户的技能和兴趣

```dart
Future<UserProfile> getUserProfile(String userId) async {
  final response = await httpService.get('/users/$userId/profile');
  // 返回包含 skills 和 interests 数组的用户资料
  return UserProfile.fromJson(response.data);
}
```

## 🎯 使用场景

### 1. 用户注册流程
- 用户注册时选择 3-5 个技能
- 选择 3-5 个兴趣爱好
- 完善个人档案

### 2. 匹配推荐
- 根据技能匹配工作机会
- 根据兴趣推荐活动和聚会
- 推荐相似兴趣的用户

### 3. 社区功能
- 按技能分组(开发者社区、设计师社区等)
- 按兴趣组织活动(徒步、摄影、编程等)
- 技能交换和学习小组

### 4. 搜索过滤
- 按技能搜索用户
- 按兴趣发现社区成员
- 组织线下见面会

## 🔐 安全性

- ✅ 所有表启用 RLS (Row Level Security)
- ✅ skills 和 interests 表对所有人可见 (只读)
- ✅ user_skills 和 user_interests 表用户可管理自己的数据
- ✅ 外键约束确保数据完整性

## 🎨 图标说明

每个技能和兴趣都配有 emoji 图标，可以在 UI 中直接使用:
- 💻 编程相关
- 🎨 设计相关
- 📱 营销相关
- 🏃 运动相关
- 🌍 旅行相关

## 📝 后续开发建议

1. **API 端点**
   - `GET /api/v1/skills` - 获取所有技能
   - `GET /api/v1/skills/:category` - 按类别获取
   - `GET /api/v1/interests` - 获取所有兴趣
   - `POST /api/v1/users/:id/skills` - 添加用户技能
   - `POST /api/v1/users/:id/interests` - 添加用户兴趣

2. **前端组件**
   - SkillsSelector - 技能选择器
   - InterestsSelector - 兴趣选择器
   - UserSkillsDisplay - 展示用户技能
   - SkillMatchIndicator - 技能匹配度

3. **搜索功能**
   - 按技能搜索用户
   - 按兴趣筛选活动
   - 智能推荐相似用户

## ✅ 完成清单

- [x] 创建 skills 表
- [x] 创建 interests 表
- [x] 创建 user_skills 关联表
- [x] 创建 user_interests 关联表
- [x] 插入 51 个技能数据
- [x] 插入 50+ 个兴趣数据
- [x] 配置 RLS 策略
- [x] 创建索引优化查询
- [x] 创建用户档案视图

---

**创建日期**: 2025-11-02  
**数据库**: Supabase PostgreSQL  
**脚本位置**: `database/migrations/insert_skills_and_interests.sql`
