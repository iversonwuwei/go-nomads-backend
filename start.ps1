# Quick Start Script for Podman Deployment
# 快速启动脚本

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "   Go-Nomads 快速部署" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if Podman is installed
Write-Host "检查 Podman 安装..." -ForegroundColor Yellow
try {
    $podmanVersion = podman --version
    Write-Host "✓ Podman 已安装: $podmanVersion" -ForegroundColor Green
}
catch {
    Write-Host "✗ 未找到 Podman，请先安装 Podman" -ForegroundColor Red
    Write-Host "  安装命令: winget install -e --id RedHat.Podman" -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "开始部署..." -ForegroundColor Yellow
Write-Host ""

# Run the main deployment script
& "$PSScriptRoot\deploy-podman.ps1" -Action start

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "   部署完成！" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "📌 访问以下地址:" -ForegroundColor Cyan
Write-Host "   Gateway:  http://localhost:5000" -ForegroundColor White
Write-Host "   Zipkin:   http://localhost:9411" -ForegroundColor White
Write-Host ""
Write-Host "📝 常用命令:" -ForegroundColor Cyan
Write-Host "   查看状态: .\deploy-podman.ps1 -Action status" -ForegroundColor White
Write-Host "   查看日志: podman logs -f go-nomads-gateway" -ForegroundColor White
Write-Host "   停止服务: .\deploy-podman.ps1 -Action stop" -ForegroundColor White
Write-Host ""
