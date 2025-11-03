# 技能和兴趣爱好 API 快速参考

## 🚀 快速开始

### 1. 数据库已初始化
✅ 技能表(skills): 51个技能
✅ 兴趣表(interests): 50+个兴趣
✅ 用户关联表已创建

### 2. 后端 API 已部署
✅ SkillsController: `/api/v1/skills`
✅ InterestsController: `/api/v1/interests`

## 📌 常用端点

### 获取所有技能(分类)
```bash
GET /api/v1/skills/by-category
```

### 获取用户技能
```bash
GET /api/v1/skills/users/{userId}
# 或使用认证
GET /api/v1/skills/me
Authorization: Bearer {token}
```

### 添加用户技能
```bash
POST /api/v1/skills/me
Authorization: Bearer {token}
{
  "skillId": "skill_javascript",
  "proficiencyLevel": "advanced",
  "yearsOfExperience": 5
}
```

### 批量添加
```bash
POST /api/v1/skills/me/batch
[
  {"skillId": "skill_python", "proficiencyLevel": "intermediate"},
  {"skillId": "skill_react", "proficiencyLevel": "expert"}
]
```

## 🎨 技能类别

- **Programming**: JavaScript, Python, React, Flutter, Go
- **Data & AI**: Machine Learning, SQL, TensorFlow
- **Design**: UI/UX Design, Figma, Photoshop
- **Marketing**: SEO, Content Writing, Social Media
- **Management**: Project Management, Agile, Leadership
- **Languages**: English, Spanish, Mandarin, Japanese
- **Technology**: Cloud Computing, Blockchain, DevOps

## 🌍 兴趣类别

- **Travel**: Backpacking, Eco-Tourism
- **Outdoor**: Hiking, Camping
- **Sports**: Surfing, Rock Climbing, Cycling
- **Culture**: Museums, Local Culture, Cooking
- **Fitness**: Yoga, Running, Gym
- **Social**: Networking, Meetups, Coworking
- **Business**: Entrepreneurship, Startups, Investing
- **Creative**: Music Production, Painting, Photography

## 📊 数据格式

### 技能熟练度
- `beginner` - 初学者
- `intermediate` - 中级
- `advanced` - 高级
- `expert` - 专家

### 兴趣强度
- `casual` - 随意
- `moderate` - 适度
- `passionate` - 热情

## 🧪 测试

```bash
# 运行完整测试
./test-skills-interests.sh

# 快速测试
curl http://localhost:5001/api/v1/skills/by-category | jq '.data[] | {category, count: (.skills | length)}'
```

## 📱 Flutter 集成示例

```dart
// 获取所有技能
final response = await httpService.get('/skills/by-category');
final skillsByCategory = (response.data as List)
    .map((c) => SkillCategory.fromJson(c))
    .toList();

// 添加用户技能
await httpService.post('/skills/me', data: {
  'skillId': 'skill_flutter',
  'proficiencyLevel': 'advanced',
  'yearsOfExperience': 4
});

// 获取当前用户技能
final mySkills = await httpService.get('/skills/me');
```

## 🔗 相关文件

- 📄 完整文档: `SKILLS_INTERESTS_API_COMPLETE.md`
- 🗄️ SQL脚本: `database/migrations/insert_skills_and_interests.sql`
- 📘 执行指南: `database/migrations/SKILLS_INTERESTS_INITIALIZATION_GUIDE.md`
- 🧪 测试脚本: `test-skills-interests.sh`

---
**更新日期**: 2025-11-02
