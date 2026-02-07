param(
    [Parameter(Mandatory = $true)]
    [string]$ServiceName,

    [Parameter(Mandatory = $false)]
    [string]$ImageName = ""
)

Write-Host "Stopping service container: $ServiceName" -ForegroundColor Cyan
try {
    $containerId = docker ps -q -f "name=$ServiceName"
    if ($containerId) {
        docker stop $containerId | Out-Null
        docker rm $containerId | Out-Null
        Write-Host "✅ Stopped and removed container $ServiceName" -ForegroundColor Green
    }
    else {
        Write-Host "ℹ️ No running container matched $ServiceName" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "⚠️ Failed to stop/remove container: $_" -ForegroundColor Yellow
}

if ($ImageName) {
    Write-Host "Removing image: $ImageName" -ForegroundColor Cyan
    try {
        docker image rm -f $ImageName | Out-Null
        Write-Host "✅ Image removed: $ImageName" -ForegroundColor Green
    }
    catch {
        Write-Host "⚠️ Failed to remove image: $_" -ForegroundColor Yellow
    }
}
