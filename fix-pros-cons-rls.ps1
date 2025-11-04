# PowerShell 脚本：修复 city_pros_cons RLS 策略
Write-Host "🚀 修复 city_pros_cons 表的 RLS 策略..." -ForegroundColor Cyan

$migrationFile = ".\fix-pros-cons-rls.sql"

if (-not (Test-Path $migrationFile)) {
    Write-Host "❌ 找不到迁移文件: $migrationFile" -ForegroundColor Red
    exit 1
}

Write-Host "📁 迁移文件: $migrationFile" -ForegroundColor Green
Write-Host ""
Write-Host "📋 SQL 内容:" -ForegroundColor Yellow
Write-Host "════════════════════════════════════════════════════════" -ForegroundColor Gray
Get-Content $migrationFile
Write-Host "════════════════════════════════════════════════════════" -ForegroundColor Gray
Write-Host ""

Write-Host "⚠️  请手动执行以下步骤:" -ForegroundColor Yellow
Write-Host ""
Write-Host "1. 打开 Supabase Dashboard SQL Editor" -ForegroundColor White
Write-Host "   https://supabase.com/dashboard/project/lcfbajrocmjlqndkrsao/sql" -ForegroundColor Cyan
Write-Host ""
Write-Host "2. SQL 内容已复制到剪贴板,直接粘贴并执行" -ForegroundColor White
Write-Host ""
Write-Host "3. 确认输出显示策略已创建" -ForegroundColor White
Write-Host ""

# 复制到剪贴板
try {
    Get-Content $migrationFile | Set-Clipboard
    Write-Host "✅ SQL 内容已复制到剪贴板!" -ForegroundColor Green
} catch {
    Write-Host "⚠️  无法复制到剪贴板，请手动复制上述 SQL 内容" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "按任意键退出..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
