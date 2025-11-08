# GeoNames 集成服务 - 快速参考

## 一分钟快速开始

### 1. 配置 Username

编辑 `appsettings.Development.json`:

```json
{
  "GeoNames": {
    "Username": "your_actual_username"  // ⚠️ 替换为您的 GeoNames username
  }
}
```

> 💡 还没有账户? 访问: <http://www.geonames.org/login>

### 2. 测试连接

```bash
# 搜索测试 (无需认证)
curl "http://localhost:5002/api/geonames/search?query=Bangkok"
```

### 3. 导入城市

```bash
# 导入泰国的所有城市 (需要 Admin Token)
curl -X POST "http://localhost:5002/api/geonames/import/country/TH" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN" \
  -d '{"overwriteExisting": true}'
```

---

## API 端点速查

| 端点 | 方法 | 认证 | 说明 |
|------|------|------|------|
| `/api/geonames/search?query={q}` | GET | ❌ | 搜索城市预览 |
| `/api/geonames/city/{name}?countryCode={code}` | GET | ❌ | 获取城市信息 |
| `/api/geonames/import/country/{code}` | POST | ✅ Admin | 按国家导入 |
| `/api/geonames/import` | POST | ✅ Admin | 完整导入 (35 国) |
| `/api/geonames/update-coordinates` | POST | ✅ Admin | 更新坐标 |

---

## 常用国家代码

| 代码 | 国家 | 代码 | 国家 | 代码 | 国家 |
|------|------|------|------|------|------|
| `TH` | 泰国 | `ID` | 印尼 | `VN` | 越南 |
| `MY` | 马来 | `PH` | 菲律宾 | `SG` | 新加坡 |
| `PT` | 葡萄牙 | `ES` | 西班牙 | `GR` | 希腊 |
| `MX` | 墨西哥 | `CO` | 哥伦比亚 | `CR` | 哥斯达黎加 |
| `US` | 美国 | `GB` | 英国 | `DE` | 德国 |
| `FR` | 法国 | `IT` | 意大利 | `JP` | 日本 |
| `AU` | 澳洲 | `NZ` | 新西兰 | `TW` | 台湾 |

---

## 配置参数速查

```json
{
  "minPopulation": 100000,      // 最小人口 (默认 10 万)
  "countryCodes": ["TH", "VN"], // 国家列表 (空=默认 35 国)
  "batchSize": 50,              // 批次大小 (默认 50)
  "overwriteExisting": true     // 覆盖已存在 (默认 false)
}
```

---

## 常见错误处理

### ❌ "Username not configured"

```bash
# 检查配置
cat appsettings.Development.json | grep -A 2 GeoNames
```

### ❌ "hourly limit exceeded"

- 等待 1 小时后重试
- 减小 `batchSize` 参数

### ❌ "City already exists"

- 设置 `"overwriteExisting": true`
- 或使用 `/update-coordinates` 仅更新坐标

---

## 数据映射速查

| GeoNames | Cities 表 | 示例 |
|----------|-----------|------|
| `name` | `Name` | "Bangkok" |
| `countryCode` | `Country` | "TH" |
| `lat` | `Latitude` | 13.75398 |
| `lng` | `Longitude` | 100.50144 |
| `population` | `Population` | 5104476 |
| `timezone.timeZoneId` | `TimeZone` | "Asia/Bangkok" |

---

## 使用示例

### 示例 1: 搜索预览

```bash
curl "http://localhost:5002/api/geonames/search?query=Bangkok"
```

### 示例 2: 导入单个国家

```bash
curl -X POST "http://localhost:5002/api/geonames/import/country/TH" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
    "overwriteExisting": true,
    "batchSize": 50
  }'
```

### 示例 3: 导入多个国家

```bash
curl -X POST "http://localhost:5002/api/geonames/import" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
    "minPopulation": 100000,
    "countryCodes": ["TH", "ID", "VN", "MY"],
    "batchSize": 50,
    "overwriteExisting": true
  }'
```

### 示例 4: 仅更新坐标

```bash
curl -X POST "http://localhost:5002/api/geonames/update-coordinates" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

---

## 响应格式

### ✅ 成功

```json
{
  "success": true,
  "data": {
    "totalProcessed": 152,
    "successCount": 152,
    "skippedCount": 0,
    "failedCount": 0
  }
}
```

### ❌ 失败

```json
{
  "success": false,
  "message": "Username not configured",
  "errors": ["Configuration error"]
}
```

---

## API 限制

- **每小时**: 1000 次请求
- **每天**: 30,000 次请求
- **内置延迟**: 100-200ms/请求

---

## 完整文档

📖 查看完整文档: `GEONAMES_INTEGRATION_GUIDE.md`

---

## 文件清单

```plaintext
CityService.Application/
├── DTOs/
│   └── GeoNamesDtos.cs                    // 数据模型
└── Services/
    ├── IGeoNamesImportService.cs          // 服务接口
    └── GeoNamesImportService.cs           // 服务实现 (545 行)

CityService/
├── Controllers/
│   └── GeoNamesController.cs              // API 控制器 (215 行)
├── Program.cs                             // 服务注册 ✅
└── appsettings.json                       // 配置模板 ✅
```

---

**版本**: 1.0.0 | **状态**: ✅ 已完成 | **更新**: 2024-01-XX
