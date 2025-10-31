# PowerShell 脚本：执行数据库迁移
Write-Host "🚀 执行数据库迁移: 添加 updated_at 字段..." -ForegroundColor Cyan

$migrationFile = ".\database\migrations\add_updated_at_to_expenses_and_photos.sql"

if (-not (Test-Path $migrationFile)) {
    Write-Host "❌ 找不到迁移文件: $migrationFile" -ForegroundColor Red
    exit 1
}

Write-Host "📁 迁移文件: $migrationFile" -ForegroundColor Green
Write-Host ""
Write-Host "📋 迁移内容预览:" -ForegroundColor Yellow
Write-Host "════════════════════════════════════════════════════════" -ForegroundColor Gray
Get-Content $migrationFile | Select-Object -First 20
Write-Host "..." -ForegroundColor Gray
Write-Host "════════════════════════════════════════════════════════" -ForegroundColor Gray
Write-Host ""

Write-Host "⚠️  请手动执行以下步骤:" -ForegroundColor Yellow
Write-Host ""
Write-Host "1. 打开 Supabase Dashboard" -ForegroundColor White
Write-Host "   https://supabase.com/dashboard/project/lcfbajrocmjlqndkrsao" -ForegroundColor Cyan
Write-Host ""
Write-Host "2. 进入 SQL Editor" -ForegroundColor White
Write-Host ""
Write-Host "3. 粘贴以下 SQL 内容并执行:" -ForegroundColor White
Write-Host "════════════════════════════════════════════════════════" -ForegroundColor Gray
Get-Content $migrationFile
Write-Host "════════════════════════════════════════════════════════" -ForegroundColor Gray
Write-Host ""
Write-Host "4. 确认输出包含: '✅ Successfully added updated_at columns'" -ForegroundColor White
Write-Host ""

# 复制到剪贴板（如果可能）
try {
    Get-Content $migrationFile | Set-Clipboard
    Write-Host "✅ SQL 内容已复制到剪贴板!" -ForegroundColor Green
    Write-Host "   直接在 Supabase SQL Editor 中粘贴即可" -ForegroundColor Gray
} catch {
    Write-Host "⚠️  无法复制到剪贴板，请手动复制上述 SQL 内容" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "按任意键退出..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
