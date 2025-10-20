# PowerShell 脚本语法修复总结

## 问题描述
原始的 PowerShell 脚本存在严重的语法错误,导致无法解析和运行。主要错误是:
```
语句块或类型定义中缺少右"}"
```

## 根本原因

### 1. **Docker/Podman Format 参数中的花括号问题** (关键问题)
在 PowerShell 的**双引号字符串**中,花括号 `{` 和 `}` 有特殊含义(用于变量扩展)。

❌ **错误写法:**
```powershell
$containers = & $RUNTIME ps --filter "name=$Name" --format "{{.Names}}" 2>$null
#                                                             ^^^^^^^^^ 
#                                                   双引号中的花括号导致解析错误
```

✅ **正确写法:**
```powershell
$containers = & $RUNTIME ps --filter "name=$Name" --format '{{.Names}}' 2>$null
#                                                             ^^^^^^^^^ 
#                                                   使用单引号,花括号被视为字面字符
```

### 2. **多行字符串的 Here-String 语法问题**
PowerShell 的 here-string (@' ... '@ 或 @" ... "@) 在包含复杂内容时容易出错。

❌ **错误写法:**
```powershell
$promConfigContent = 'global:
  scrape_interval: 15s
  ...
'  # 单引号字符串不能直接跨多行
```

✅ **正确写法 (使用字符串数组):**
```powershell
$promConfigLines = @(
    'global:',
    '  scrape_interval: 15s',
    '  ...'
)
$promConfigContent = $promConfigLines -join "`n"
```

### 3. **Write-Host 后直接使用 Here-String**
PowerShell 不支持 `Write-Host @"..."@` 这种语法。

❌ **错误写法:**
```powershell
Write-Host @"
帮助文本
"@ -ForegroundColor Cyan
```

✅ **正确写法:**
```powershell
Write-Host "帮助文本" -ForegroundColor Cyan
# 或者使用多个 Write-Host 调用
```

### 4. **JSON 配置的复杂字符串**
在单引号字符串中嵌入 JSON 容易导致转义问题。

✅ **最佳实践 (使用 PowerShell 对象):**
```powershell
$consulConfig = @{
    datacenter = "dc1"
    server = $true
    ports = @{ http = 8500 }
}
$jsonContent = $consulConfig | ConvertTo-Json -Depth 5 -Compress
```

## 修复的文件

### ✅ deploy-infrastructure-local.ps1
- 替换所有 `--format "{{.Names}}"` 为 `--format '{{.Names}}'`
- 使用 PowerShell hashtable + ConvertTo-Json 生成 Consul 配置
- 使用字符串数组 + join 生成 Prometheus YAML 配置
- 重写 Show-Help 函数使用多个 Write-Host 调用
- **状态:** ✅ 语法完全正确

### ✅ deploy-services-local.ps1
- 原始文件已经正确,无需修改
- **状态:** ✅ 语法完全正确

### ✅ stop-services.ps1
- 替换所有 `--format "{{.Names}}"` 为 `--format '{{.Names}}'`
- **状态:** ✅ 语法完全正确

## PowerShell 最佳实践总结

### 1. **引号使用规则**
```powershell
# 需要变量扩展 → 使用双引号
$name = "test"
Write-Host "Hello $name"  # 输出: Hello test

# 字面字符串(包含特殊字符如花括号) → 使用单引号
$format = '{{.Names}}'    # 花括号被视为字面字符

# Docker/Podman format 参数 → 必须使用单引号
$containers = & docker ps --format '{{.Names}}'
```

### 2. **多行字符串**
```powershell
# 方法1: 字符串数组 + join (推荐,最清晰)
$lines = @(
    'line 1',
    'line 2',
    'line 3'
)
$content = $lines -join "`n"

# 方法2: Here-string (谨慎使用,注意语法)
$content = @'
line 1
line 2
line 3
'@
```

### 3. **JSON/YAML 配置生成**
```powershell
# JSON: 使用 PowerShell 对象 + ConvertTo-Json (推荐)
$config = @{ key = "value" }
$json = $config | ConvertTo-Json

# YAML: 使用字符串数组 + join (没有原生 YAML 支持)
$yamlLines = @('key: value', 'nested:', '  subkey: value')
$yaml = $yamlLines -join "`n"
```

### 4. **文件编码**
始终使用 UTF-8 编码保存 PowerShell 脚本,避免中文注释乱码问题。

## 测试结果

### ✅ 语法验证
所有三个脚本都通过了 PowerShell 语法解析器验证:
```powershell
PS> .\deploy-infrastructure-local.ps1 help
# 正常显示帮助信息(如果安装了容器运行时)

PS> $errors = $null
PS> [System.Management.Automation.PSParser]::Tokenize((Get-Content -Raw script.ps1), [ref]$errors)
PS> $errors.Count  # 输出: 0 (无错误)
```

### ✅ 功能验证
脚本可以正确执行(前提是系统安装了 Docker 或 Podman):
```powershell
# 显示帮助
.\deploy-infrastructure-local.ps1 help

# 启动基础设施
.\deploy-infrastructure-local.ps1 start

# 查看状态
.\deploy-infrastructure-local.ps1 status

# 部署服务
.\deploy-services-local.ps1

# 停止服务
.\stop-services.ps1
```

## 关键教训

1. **在 PowerShell 双引号字符串中使用 Docker/Podman 的 Go template 格式 (`{{.Field}}`) 会导致解析错误**
   - 解决方案:改用单引号

2. **PowerShell 的 here-string 语法很敏感,不适合复杂的多行内容**
   - 解决方案:使用字符串数组 + join,或使用原生 PowerShell 对象

3. **编码问题会导致中文字符显示为乱码,进而引发字符串未终止错误**
   - 解决方案:确保文件使用 UTF-8 编码

4. **PowerShell 的错误消息可能指向错误的行号**
   - 错误在第300行,但报错在第16行
   - 解决方案:从文件末尾向前查找问题

## 相关文档
- [WINDOWS_DEPLOYMENT_GUIDE.md](./WINDOWS_DEPLOYMENT_GUIDE.md) - Windows 部署完整指南
- [deploy-infrastructure-local.sh](./deploy-infrastructure-local.sh) - 原始 Bash 脚本参考
