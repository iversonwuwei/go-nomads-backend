# 城市评分系统 - 数据库迁移指南

## ⚠️ 重要：必须先执行此SQL才能使用评分功能

### 执行步骤：

1. **打开 Supabase Dashboard**
   - 访问：https://supabase.com/dashboard
   - 选择你的项目

2. **进入 SQL Editor**
   - 左侧菜单点击 "SQL Editor"
   - 点击 "New query" 创建新查询

3. **复制并执行 SQL**
   - 打开文件：`city_rating_system.sql`
   - 复制全部内容（73行）
   - 粘贴到 Supabase SQL Editor
   - 点击 "Run" 按钮执行

4. **验证表创建成功**
   执行以下查询确认：
   ```sql
   -- 检查表是否创建
   SELECT table_name 
   FROM information_schema.tables 
   WHERE table_name IN ('city_rating_categories', 'city_ratings');
   
   -- 检查默认评分项是否插入
   SELECT COUNT(*) as category_count 
   FROM city_rating_categories;
   -- 应该返回 10
   ```

5. **重启 CityService**
   ```bash
   cd /Users/walden/Workspaces/WaldenProjects/go-noma/deployment
   ./deploy-services-local.sh
   ```

## 创建的表结构：

### 1. city_rating_categories (评分项表)
- 10个默认评分项
- 支持用户自定义评分项
- 包含图标、排序等字段

### 2. city_ratings (用户评分表)
- 存储用户对城市的评分
- 每个用户对每个城市的每个评分项只能评一次
- 评分范围：0-5星

### 3. city_rating_statistics (视图)
- 自动计算每个评分项的平均分
- 统计评分人数
- 按显示顺序排序

## 问题排查：

如果执行后仍然看不到数据：
1. 确认 Supabase 连接字符串正确
2. 检查后端日志中的错误信息
3. 查看浏览器控制台的API响应
4. 确认用户已登录（评分需要认证）

