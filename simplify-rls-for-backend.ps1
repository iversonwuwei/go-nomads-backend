# PowerShell 脚本：简化 RLS 策略
Write-Host "🔧 简化数据库 RLS 策略..." -ForegroundColor Cyan
Write-Host "   原则: 信任后端应用层的身份验证" -ForegroundColor Gray
Write-Host ""

$migrationFile = ".\simplify-rls-for-backend.sql"

if (-not (Test-Path $migrationFile)) {
    Write-Host "❌ 找不到文件: $migrationFile" -ForegroundColor Red
    exit 1
}

Write-Host "📋 SQL 内容预览:" -ForegroundColor Yellow
Write-Host "════════════════════════════════════════════════════════" -ForegroundColor Gray
Get-Content $migrationFile | Select-Object -First 30
Write-Host "..." -ForegroundColor Gray
Write-Host "════════════════════════════════════════════════════════" -ForegroundColor Gray
Write-Host ""

Write-Host "📌 简化说明:" -ForegroundColor Yellow
Write-Host "   ✅ 保留 SELECT 策略(所有人可读)" -ForegroundColor Green
Write-Host "   ✅ 简化 INSERT/UPDATE/DELETE 策略" -ForegroundColor Green
Write-Host "   ✅ 允许 authenticated 和 service_role 角色操作" -ForegroundColor Green
Write-Host "   ✅ 具体权限控制由后端应用层负责" -ForegroundColor Green
Write-Host ""

Write-Host "⚠️  请手动执行以下步骤:" -ForegroundColor Yellow
Write-Host ""
Write-Host "1. 打开 Supabase Dashboard SQL Editor" -ForegroundColor White
Write-Host "   https://supabase.com/dashboard/project/lcfbajrocmjlqndkrsao/sql" -ForegroundColor Cyan
Write-Host ""
Write-Host "2. SQL 内容已复制到剪贴板,直接粘贴并执行" -ForegroundColor White
Write-Host ""
Write-Host "3. 确认输出显示所有策略已更新" -ForegroundColor White
Write-Host ""

# 复制到剪贴板
try {
    Get-Content $migrationFile | Set-Clipboard
    Write-Host "✅ SQL 内容已复制到剪贴板!" -ForegroundColor Green
} catch {
    Write-Host "⚠️  无法复制到剪贴板，请手动复制" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "执行完成后,无需重启服务,立即生效! ⚡" -ForegroundColor Green
Write-Host ""
Write-Host "按任意键退出..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
