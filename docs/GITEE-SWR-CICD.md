# Gitee Go + 华为云 SWR CI/CD 配置指南

本文档说明如何配置 Go-Nomads 项目的 Gitee Go CI/CD 流水线，将 Docker 镜像推送到华为云 SWR（Software Repository for Container）。

## 📋 目录

1. [前提条件](#前提条件)
2. [华为云 SWR 配置](#华为云-swr-配置)
3. [Gitee 仓库配置](#gitee-仓库配置)
4. [流水线配置文件](#流水线配置文件)
5. [本地测试](#本地测试)
6. [故障排除](#故障排除)

---

## 前提条件

- [x] 华为云账号（已开通 SWR 服务）
- [x] Gitee 账号（代码仓库）
- [x] Docker 环境（用于本地测试）

---

## 华为云 SWR 配置

### 1. 创建 SWR 组织

1. 登录 [华为云控制台](https://console.huaweicloud.com/)
2. 进入 **容器镜像服务 SWR**
3. 点击 **组织管理** -> **创建组织**
4. 输入组织名称（如 `go-nomads`）

### 2. 获取访问凭证

1. 进入 **我的凭证** -> **访问密钥**
2. 点击 **新增访问密钥**
3. 下载并保存 `credentials.csv` 文件
4. 记录以下信息：
   - **Access Key ID (AK)**
   - **Secret Access Key (SK)**

### 3. 获取 SWR 登录信息

SWR 登录服务器格式：`swr.<区域>.myhuaweicloud.com`

常用区域：
| 区域 | 登录服务器 |
|------|-----------|
| 华北-北京四 | swr.cn-north-4.myhuaweicloud.com |
| 华东-上海一 | swr.cn-east-3.myhuaweicloud.com |
| 华南-广州 | swr.cn-south-1.myhuaweicloud.com |
| 亚太-香港 | swr.ap-southeast-1.myhuaweicloud.com |

---

## Gitee 仓库配置

### 1. 配置流水线环境变量

进入 Gitee 仓库 -> **设置** -> **流水线** -> **变量**

添加以下变量（均设置为 **加密** 类型）：

| 变量名 | 说明 | 示例值 |
|--------|------|--------|
| `SWR_REGION` | 华为云区域 | `cn-north-4` |
| `SWR_AK` | Access Key | `AKIA...` |
| `SWR_SK` | Secret Key | `wJal...` |
| `SWR_ORGANIZATION` | SWR 组织名称 | `go-nomads` |
| `SWR_LOGIN_SERVER` | SWR 登录服务器 | `swr.cn-north-4.myhuaweicloud.com` |

### 2. 启用 Gitee Go

1. 进入仓库 -> **流水线**
2. 点击 **开通服务**（如未开通）
3. 选择流水线配置方式为 **YAML 配置**

---

## 流水线配置文件

### Backend 项目结构

```
go-nomads-backend/
├── .workflow/
│   └── ci-cd.yml          # Gitee Go 流水线配置
├── scripts/
│   └── gitee-swr-build.sh # 本地构建脚本
├── .env.swr.template      # 环境变量模板
└── src/
    ├── Gateway/
    └── Services/
```

### Web 项目结构

```
go-nomads-web/
├── .workflow/
│   └── ci-cd.yml          # Gitee Go 流水线配置
├── Dockerfile
└── src/
```

### 流水线触发条件

流水线在以下情况自动触发：

- 推送到 `main`/`master`/`develop` 分支
- 推送到 `release/*` 分支
- 创建 `v*` 标签

---

## 本地测试

### 1. 配置环境变量

```bash
# 复制模板文件
cp .env.swr.template .env.swr

# 编辑并填入实际值
vim .env.swr
```

### 2. 运行构建脚本

```bash
# 给脚本添加执行权限
chmod +x scripts/gitee-swr-build.sh

# 查看帮助
./scripts/gitee-swr-build.sh help

# 列出所有服务
./scripts/gitee-swr-build.sh list

# 构建并推送单个服务
./scripts/gitee-swr-build.sh gateway
./scripts/gitee-swr-build.sh user-service

# 构建并推送所有服务
./scripts/gitee-swr-build.sh all
```

### 3. 手动 Docker 命令

```bash
# 登录 SWR
docker login -u "cn-north-4@${AK}" -p "${SK}" swr.cn-north-4.myhuaweicloud.com

# 构建镜像
docker build -f src/Gateway/Gateway/Dockerfile -t swr.cn-north-4.myhuaweicloud.com/go-nomads/gateway:latest .

# 推送镜像
docker push swr.cn-north-4.myhuaweicloud.com/go-nomads/gateway:latest
```

---

## 服务列表

| 服务名 | 镜像名称 | Dockerfile 路径 |
|--------|----------|-----------------|
| Gateway | gateway | src/Gateway/Gateway/Dockerfile |
| UserService | user-service | src/Services/UserService/UserService/Dockerfile |
| CityService | city-service | src/Services/CityService/CityService/Dockerfile |
| AccommodationService | accommodation-service | src/Services/AccommodationService/AccommodationService/Dockerfile |
| CoworkingService | coworking-service | src/Services/CoworkingService/CoworkingService/Dockerfile |
| EventService | event-service | src/Services/EventService/EventService/Dockerfile |
| AIService | ai-service | src/Services/AIService/AIService/Dockerfile |
| MessageService | message-service | src/Services/MessageService/MessageService/API/Dockerfile |
| SearchService | search-service | src/Services/SearchService/SearchService/Dockerfile |
| CacheService | cache-service | src/Services/CacheService/CacheService/Dockerfile |
| InnovationService | innovation-service | src/Services/InnovationService/InnovationService/Dockerfile |
| ProductService | product-service | src/Services/ProductService/ProductService/Dockerfile |
| DocumentService | document-service | src/Services/DocumentService/DocumentService/Dockerfile |
| Web | go-nomads-web | (go-nomads-web 仓库) Dockerfile |

---

## 故障排除

### 1. 登录 SWR 失败

**错误**: `unauthorized: authentication required`

**解决方案**:
- 检查 AK/SK 是否正确
- 确认区域代码正确（如 `cn-north-4`）
- 登录格式：`${REGION}@${AK}` 作为用户名

### 2. 推送镜像失败

**错误**: `denied: requested access to the resource is denied`

**解决方案**:
- 检查 SWR 组织名称是否存在
- 确认 IAM 用户有 SWR 的推送权限
- 检查镜像命名格式是否正确

### 3. 构建超时

**解决方案**:
- 检查网络连接
- 使用华为云内网地址（如果在华为云 ECS 上运行）
- 配置 Docker 镜像加速器

### 4. Dockerfile 不存在

**解决方案**:
- 确认在项目根目录运行命令
- 检查 Dockerfile 路径是否正确
- 运行 `ls -la src/` 查看目录结构

---

## 进阶配置

### 添加钉钉/飞书通知

在流水线最后一个阶段添加通知步骤：

```yaml
- step: shell@agent
  name: notify
  script:
    - |
      curl -X POST -H "Content-Type: application/json" \
        -d "{\"msgtype\": \"text\", \"text\": {\"content\": \"Go-Nomads 构建成功\\n分支: ${GITEE_BRANCH}\\n提交: ${GITEE_COMMIT_SHA}\"}}" \
        ${DINGTALK_WEBHOOK_URL}
```

### 配置 Kubernetes 自动部署

```yaml
- step: shell@agent
  name: deploy_to_k8s
  script:
    - |
      kubectl set image deployment/gateway \
        gateway=${SWR_LOGIN_SERVER}/${SWR_ORGANIZATION}/gateway:${GITEE_COMMIT_SHA:0:8} \
        -n go-nomads
```

---

## 相关链接

- [Gitee Go 官方文档](https://gitee.com/help/articles/4320)
- [华为云 SWR 文档](https://support.huaweicloud.com/swr/index.html)
- [Docker 官方文档](https://docs.docker.com/)
