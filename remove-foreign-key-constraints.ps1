#!/usr/bin/env pwsh
# =====================================================
# 删除外键约束执行脚本 - 推荐方案
# =====================================================

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "删除外键约束 - PowerShell 执行脚本" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host ""

# 读取 SQL 文件
$sqlFile = Join-Path $PSScriptRoot "remove-foreign-key-constraints.sql"

if (-not (Test-Path $sqlFile)) {
    Write-Host "❌ 错误: 找不到 SQL 文件: $sqlFile" -ForegroundColor Red
    exit 1
}

$sqlContent = Get-Content $sqlFile -Raw -Encoding UTF8

# 复制到剪贴板
Set-Clipboard -Value $sqlContent

Write-Host "✅ SQL 脚本已复制到剪贴板!" -ForegroundColor Green
Write-Host ""
Write-Host "📋 为什么删除外键约束?" -ForegroundColor Yellow
Write-Host "   1. 应用层已有完善的 JWT 身份验证" -ForegroundColor White
Write-Host "   2. Token 中的 user_id 可能不在 public.users 表中" -ForegroundColor White
Write-Host "   3. 测试环境数据经常清空,导致外键冲突" -ForegroundColor White
Write-Host ""
Write-Host "📋 执行步骤:" -ForegroundColor Yellow
Write-Host "1. 打开 Supabase SQL Editor:" -ForegroundColor White
Write-Host "   https://supabase.com/dashboard/project/lcfbajrocmjlqndkrsao/sql" -ForegroundColor Cyan
Write-Host ""
Write-Host "2. 粘贴 SQL (Ctrl+V) 并点击 'Run'" -ForegroundColor White
Write-Host ""
Write-Host "3. 检查验证结果,应该没有任何行返回(所有外键已删除)" -ForegroundColor White
Write-Host ""
Write-Host "4. 重新测试添加 Pros & Cons" -ForegroundColor White
Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
