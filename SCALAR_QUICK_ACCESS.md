# Scalar 文档快速访问

## 📚 文档中心

### 主文档门户
🌟 **DocumentService** - 统一文档中心  
**地址**: http://localhost:5003/scalar/v1  
**说明**: 聚合所有服务的 API 文档

---

## 🎯 各服务文档

### 1. Gateway
**地址**: http://localhost:5000/scalar/v1  
**主题**: Saturn (土星主题)  
**端口**: 5000  
**功能**: API 网关路由文档

### 2. Product Service
**地址**: http://localhost:5001/scalar/v1  
**主题**: Mars (火星主题)  
**端口**: 5001  
**功能**: 产品服务 API 文档

### 3. User Service
**地址**: http://localhost:5002/scalar/v1  
**主题**: BluePlanet (蓝色星球)  
**端口**: 5002  
**功能**: 用户服务 API 文档

### 4. Document Service
**地址**: http://localhost:5003/scalar/v1  
**主题**: Purple (紫色主题)  
**端口**: 5003  
**功能**: 文档服务自身 API

---

## 🔧 API 端点

### 服务列表
```bash
curl http://localhost:5003/api/services
```

### 聚合的 OpenAPI 规范
```bash
curl http://localhost:5003/api/specs
```

### 健康检查
```bash
# Gateway
curl http://localhost:5000/health

# Product Service
curl http://localhost:5001/health

# User Service
curl http://localhost:5002/health

# Document Service
curl http://localhost:5003/health
```

---

## ⌨️ Scalar UI 快捷键

- **Ctrl/Cmd + K** - 打开搜索
- **Tab** - 在界面元素间导航
- **Enter** - 展开/折叠 API 端点

---

## 🎨 主题说明

| 服务 | 主题 | 颜色特点 |
|------|------|---------|
| Gateway | Saturn | 橙黄色调 |
| Product Service | Mars | 红橙色调 |
| User Service | BluePlanet | 蓝色调 |
| Document Service | Purple | 紫色调 |

---

## ✅ 验证状态

已部署服务:
- ✅ Gateway - http://localhost:5000/scalar/v1
- ✅ Product Service - http://localhost:5001/scalar/v1
- ✅ User Service - http://localhost:5002/scalar/v1
- ✅ Document Service - http://localhost:5003/scalar/v1

所有服务的 Scalar UI 已成功启动!🎉
