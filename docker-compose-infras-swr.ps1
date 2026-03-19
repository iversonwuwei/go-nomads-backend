param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$ComposeArgs
)

$ErrorActionPreference = 'Stop'

$ProjectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$ComposeFile = Join-Path $ProjectRoot 'docker-compose-infras-swr.yml'
$DefaultServices = @('redis', 'rabbitmq', 'elasticsearch')
$UseSwr = $false

if (-not (Test-Path $ComposeFile)) {
    throw "Compose file not found: $ComposeFile"
}

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    throw 'Docker is required.'
}

function Set-SwrImages {
    $registry = if ($env:SWR_REGISTRY) { $env:SWR_REGISTRY } else { 'swr.ap-southeast-3.myhuaweicloud.com' }
    $organization = if ($env:SWR_ORGANIZATION) { $env:SWR_ORGANIZATION } else { 'go-nomads' }

    $env:REDIS_IMAGE = "$registry/$organization/redis:7-alpine"
    $env:RABBITMQ_IMAGE = "$registry/$organization/rabbitmq:3-management-alpine"
    $env:ELASTICSEARCH_IMAGE = "$registry/$organization/elasticsearch:8.11.0"
    $env:CONSUL_IMAGE = "$registry/$organization/consul:latest"
}

$RemainingArgs = @()
foreach ($arg in $ComposeArgs) {
    switch ($arg) {
        '--use-swr' {
            $UseSwr = $true
        }
        '--use-official' {
            $UseSwr = $false
        }
        default {
            $RemainingArgs += $arg
        }
    }
}

$ComposeArgs = $RemainingArgs

if ($UseSwr) {
    Set-SwrImages
}

if (-not $ComposeArgs -or $ComposeArgs.Count -eq 0) {
    $ComposeArgs = @('up', '-d') + $DefaultServices
} elseif ($ComposeArgs[0] -eq '--with-consul') {
    if ($ComposeArgs.Count -eq 1) {
        $ComposeArgs = @('up', '-d') + $DefaultServices + @('consul')
    } else {
        $ComposeArgs = $ComposeArgs[1..($ComposeArgs.Count - 1)]
    }
}

$Registry = if ($env:SWR_REGISTRY) { $env:SWR_REGISTRY } else { 'swr.ap-southeast-3.myhuaweicloud.com' }
$Organization = if ($env:SWR_ORGANIZATION) { $env:SWR_ORGANIZATION } else { 'go-nomads' }

Write-Host "Using compose file: $ComposeFile"
Write-Host "Image source: $(if ($UseSwr) { 'swr' } else { 'official' })"
Write-Host "Registry: $Registry"
Write-Host "Org: $Organization"
Write-Host "Default services: $($DefaultServices -join ', ')"

& docker compose -f $ComposeFile @ComposeArgs
