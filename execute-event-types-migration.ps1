# 执行聚会类型表创建 SQL
# 需要先设置环境变量 SUPABASE_DB_URL

param(
    [string]$SqlFile = "src/Services/EventService/EventService/Database/create-event-types-table.sql"
)

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "创建聚会类型表 (event_types)" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# 检查 SQL 文件是否存在
if (-not (Test-Path $SqlFile)) {
    Write-Host "❌ SQL 文件不存在: $SqlFile" -ForegroundColor Red
    exit 1
}

Write-Host "📄 SQL 文件: $SqlFile" -ForegroundColor Green
Write-Host ""

# 检查环境变量
if (-not $env:SUPABASE_DB_URL) {
    Write-Host "❌ 未设置 SUPABASE_DB_URL 环境变量" -ForegroundColor Red
    Write-Host ""
    Write-Host "请设置数据库连接字符串:" -ForegroundColor Yellow
    Write-Host '  $env:SUPABASE_DB_URL = "postgresql://postgres.xxx:password@host:port/postgres"' -ForegroundColor Gray
    Write-Host ""
    exit 1
}

Write-Host "🔗 数据库连接: $($env:SUPABASE_DB_URL -replace 'password=[^@]+', 'password=***')" -ForegroundColor Green
Write-Host ""

# 读取 SQL 文件内容
$sqlContent = Get-Content $SqlFile -Raw -Encoding UTF8

Write-Host "📋 SQL 内容预览:" -ForegroundColor Yellow
Write-Host "---" -ForegroundColor DarkGray
Write-Host $sqlContent.Substring(0, [Math]::Min(500, $sqlContent.Length)) -ForegroundColor Gray
if ($sqlContent.Length -gt 500) {
    Write-Host "... (还有 $($sqlContent.Length - 500) 个字符)" -ForegroundColor DarkGray
}
Write-Host "---" -ForegroundColor DarkGray
Write-Host ""

# 确认执行
$confirm = Read-Host "是否执行此 SQL？(y/n)"
if ($confirm -ne 'y') {
    Write-Host "❌ 已取消" -ForegroundColor Yellow
    exit 0
}

Write-Host ""
Write-Host "🚀 开始执行 SQL..." -ForegroundColor Cyan
Write-Host ""

try {
    # 使用 psql 执行 SQL（需要安装 PostgreSQL 客户端工具）
    if (Get-Command psql -ErrorAction SilentlyContinue) {
        $sqlContent | psql $env:SUPABASE_DB_URL
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host ""
            Write-Host "✅ SQL 执行成功！" -ForegroundColor Green
            Write-Host ""
            Write-Host "📊 已创建:" -ForegroundColor Cyan
            Write-Host "  ✓ event_types 表" -ForegroundColor Green
            Write-Host "  ✓ 索引和唯一约束" -ForegroundColor Green
            Write-Host "  ✓ RLS 策略" -ForegroundColor Green
            Write-Host "  ✓ 20 个预设聚会类型" -ForegroundColor Green
            Write-Host ""
        } else {
            Write-Host ""
            Write-Host "❌ SQL 执行失败" -ForegroundColor Red
            Write-Host "退出代码: $LASTEXITCODE" -ForegroundColor Red
            exit 1
        }
    } else {
        Write-Host "❌ 未找到 psql 命令" -ForegroundColor Red
        Write-Host ""
        Write-Host "请安装 PostgreSQL 客户端工具，或手动执行 SQL：" -ForegroundColor Yellow
        Write-Host "1. 打开 Supabase SQL Editor" -ForegroundColor Gray
        Write-Host "2. 复制 $SqlFile 的内容" -ForegroundColor Gray
        Write-Host "3. 粘贴并执行" -ForegroundColor Gray
        Write-Host ""
        exit 1
    }
} catch {
    Write-Host "❌ 执行出错: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "完成！" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "📝 下一步:" -ForegroundColor Green
Write-Host "  1. 启动 EventService" -ForegroundColor Gray
Write-Host "  2. 运行 ./test-event-types.ps1 测试 API" -ForegroundColor Gray
Write-Host ""
