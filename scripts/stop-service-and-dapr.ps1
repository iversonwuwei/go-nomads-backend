param(
    [Parameter(Mandatory = $true)]
    [string]$ServiceName,

    [Parameter(Mandatory = $false)]
    [string]$AppId = $ServiceName,

    [Parameter(Mandatory = $true)]
    [string]$ImageName
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

Write-Host "Stopping Dapr app: $AppId" -ForegroundColor Cyan
try {
    dapr stop --app-id $AppId | Out-Null
    Write-Host "✅ Dapr app stopped (if it was running)" -ForegroundColor Green
}
catch {
    Write-Host "⚠️ Failed to stop Dapr app: $_" -ForegroundColor Yellow
}

Write-Host "Removing image: $ImageName" -ForegroundColor Cyan
try {
    docker image rm -f $ImageName | Out-Null
    Write-Host "✅ Image removed: $ImageName" -ForegroundColor Green
}
catch {
    Write-Host "⚠️ Failed to remove image: $_" -ForegroundColor Yellow
}
