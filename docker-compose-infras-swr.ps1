param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$ComposeArgs
)

$ErrorActionPreference = 'Stop'

$ProjectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$ComposeFile = Join-Path $ProjectRoot 'docker-compose-infras-swr.yml'
$DefaultServices = @('redis', 'rabbitmq', 'elasticsearch', 'nginx')
$UseSwr = $false
$UseMirror = $false
$MirrorPrefix = if ($env:MIRROR_PREFIX) { $env:MIRROR_PREFIX } else { 'docker.1ms.run' }

if (-not (Test-Path $ComposeFile)) {
    throw "Compose file not found: $ComposeFile"
}

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    throw 'Docker is required.'
}

function Set-SwrImages {
    $registry = if ($env:SWR_LOGIN_SERVER) { $env:SWR_LOGIN_SERVER } elseif ($env:SWR_REGISTRY) { $env:SWR_REGISTRY } else { 'swr.ap-southeast-3.myhuaweicloud.com' }
    $organization = if ($env:SWR_ORGANIZATION) { $env:SWR_ORGANIZATION } else { 'go-nomads' }

    $env:REDIS_IMAGE = "$registry/$organization/redis:7.4"
    $env:RABBITMQ_IMAGE = "$registry/$organization/rabbitmq:3-management-alpine"
    $env:ELASTICSEARCH_IMAGE = "$registry/$organization/elasticsearch:8.17.4"
    $env:NGINX_IMAGE = "$registry/$organization/nginx:1.29.6"
}

function Set-MirrorImages {
    $env:REDIS_IMAGE = "$MirrorPrefix/library/redis:7.4"
    $env:RABBITMQ_IMAGE = "$MirrorPrefix/library/rabbitmq:3-management-alpine"
    $env:ELASTICSEARCH_IMAGE = "$(if ($env:SWR_LOGIN_SERVER) { $env:SWR_LOGIN_SERVER } elseif ($env:SWR_REGISTRY) { $env:SWR_REGISTRY } else { 'swr.ap-southeast-3.myhuaweicloud.com' })/$((if ($env:SWR_ORGANIZATION) { $env:SWR_ORGANIZATION } else { 'go-nomads' }))/elasticsearch:8.17.4"
    $env:NGINX_IMAGE = "$MirrorPrefix/library/nginx:1.29.6"
}

$RemainingArgs = @()
foreach ($arg in $ComposeArgs) {
    switch ($arg) {
        '--use-swr' {
            $UseSwr = $true
            $UseMirror = $false
        }
        '--use-mirror' {
            $UseMirror = $true
            $UseSwr = $false
        }
        '--use-official' {
            $UseSwr = $false
            $UseMirror = $false
        }
        default {
            $RemainingArgs += $arg
        }
    }
}

$ComposeArgs = $RemainingArgs

if ($UseSwr) {
    Set-SwrImages
} elseif ($UseMirror) {
    Set-MirrorImages
}

if (-not $ComposeArgs -or $ComposeArgs.Count -eq 0) {
    $ComposeArgs = @('up', '-d') + $DefaultServices
}

$Registry = if ($env:SWR_LOGIN_SERVER) { $env:SWR_LOGIN_SERVER } elseif ($env:SWR_REGISTRY) { $env:SWR_REGISTRY } else { 'swr.ap-southeast-3.myhuaweicloud.com' }
$Organization = if ($env:SWR_ORGANIZATION) { $env:SWR_ORGANIZATION } else { 'go-nomads' }

Write-Host "Using compose file: $ComposeFile"
Write-Host "Image source: $(if ($UseSwr) { 'swr' } elseif ($UseMirror) { 'mirror' } else { 'official' })"
Write-Host "Registry: $Registry"
Write-Host "Mirror prefix: $MirrorPrefix"
Write-Host "Org: $Organization"
Write-Host "Default services: $($DefaultServices -join ', ')"

& docker compose -f $ComposeFile @ComposeArgs
