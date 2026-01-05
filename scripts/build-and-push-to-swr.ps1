# ============================================================
# Build Docker images and push to Huawei Cloud SWR (PowerShell)
# ============================================================

[CmdletBinding()]
param(
    [switch]$Help,
    [switch]$Login,
    [switch]$BuildOnly,
    [switch]$PushOnly,
    [switch]$All,
    [string]$Tag = "latest",
    [switch]$List,
    [Parameter(Position = 0, ValueFromRemainingArguments = $true)]
    [string[]]$Services
)

$ErrorActionPreference = "Stop"

# ============================================================
# Configuration
# ============================================================
$SWR_REGISTRY = if ($env:SWR_REGISTRY) { $env:SWR_REGISTRY } else { "swr.ap-southeast-3.myhuaweicloud.com" }
$SWR_ORGANIZATION = if ($env:SWR_ORGANIZATION) { $env:SWR_ORGANIZATION } else { "go-nomads" }
$IMAGE_TAG = $Tag

# Project root directory
$PROJECT_ROOT = Split-Path -Parent $PSScriptRoot

# Services map: service name -> Dockerfile path
$SERVICES_MAP = @{
    "gateway"                  = "src/Gateway/Gateway/Dockerfile"
    "user-service"             = "src/Services/UserService/UserService/Dockerfile"
    "city-service"             = "src/Services/CityService/CityService/Dockerfile"
    "coworking-service"        = "src/Services/CoworkingService/CoworkingService/Dockerfile"
    "accommodation-service"    = "src/Services/AccommodationService/AccommodationService/Dockerfile"
    "event-service"            = "src/Services/EventService/EventService/Dockerfile"
    "ai-service"               = "src/Services/AIService/AIService/Dockerfile"
    "cache-service"            = "src/Services/CacheService/CacheService/Dockerfile"
    "document-service"         = "src/Services/DocumentService/DocumentService/Dockerfile"
    "ecommerce-service"        = "src/Services/EcommerceService/EcommerceService/Dockerfile"
    "innovation-service"       = "src/Services/InnovationService/InnovationService/Dockerfile"
    "message-service"          = "src/Services/MessageService/MessageService/API/Dockerfile"
    "product-service"          = "src/Services/ProductService/ProductService/Dockerfile"
    "travel-planning-service"  = "src/Services/TravelPlanningService/TravelPlanningService/Dockerfile"
}

# ============================================================
# Functions
# ============================================================

function Show-Help {
    Write-Host @"
Usage: .\build-and-push-to-swr.ps1 [options] [service names...]

Options:
  -Help              Show this help message
  -Login             Login to SWR registry
  -BuildOnly         Build images only, do not push
  -PushOnly          Push images only, do not build
  -All               Build and push all services
  -Tag <tag>         Specify image tag (default: latest)
  -List              List all available services

Environment Variables:
  SWR_REGISTRY       SWR registry URL (default: swr.ap-southeast-3.myhuaweicloud.com)
  SWR_ORGANIZATION   SWR organization name (default: go-nomads)
  SWR_AK             Huawei Cloud Access Key (for login)
  SWR_SK             Huawei Cloud Secret Key (for login)
  SWR_REGION         Huawei Cloud region (for login, default: ap-southeast-3)

Examples:
  .\build-and-push-to-swr.ps1 -Login                        # Login to SWR
  .\build-and-push-to-swr.ps1 gateway                       # Build and push gateway
  .\build-and-push-to-swr.ps1 -Tag v1.0.0 gateway user-service  # Use v1.0.0 tag
  .\build-and-push-to-swr.ps1 -All                          # Build and push all services
  .\build-and-push-to-swr.ps1 -BuildOnly gateway            # Build gateway only
"@
}

function Show-ServiceList {
    Write-Host "Available services:"
    Write-Host "==================="
    foreach ($service in $SERVICES_MAP.Keys | Sort-Object) {
        Write-Host "  - $service"
    }
}

function Invoke-SwrLogin {
    Write-Host "================================================"
    Write-Host "Login to Huawei Cloud SWR: $SWR_REGISTRY"
    Write-Host "================================================"
    
    $swrAk = $env:SWR_AK
    $swrSk = $env:SWR_SK
    $swrRegion = if ($env:SWR_REGION) { $env:SWR_REGION } else { "ap-southeast-3" }
    
    if ($swrAk -and $swrSk) {
        Write-Host "Logging in with AK/SK..."
        $username = "${swrRegion}@${swrAk}"
        docker login -u $username -p $swrSk $SWR_REGISTRY
        if ($LASTEXITCODE -ne 0) {
            throw "Login failed"
        }
        Write-Host "[OK] Login successful" -ForegroundColor Green
    }
    else {
        Write-Host @"
Please set SWR_AK and SWR_SK environment variables, or login manually.

Method 1: Use AK/SK
  `$env:SWR_AK = "<your-access-key>"
  `$env:SWR_SK = "<your-secret-key>"
  .\build-and-push-to-swr.ps1 -Login

Method 2: Get temporary login command from Huawei Cloud Console
  Go to: Container Image Service -> My Images -> Client Upload
  Copy and execute the login command

Method 3: Manual docker login
  docker login $SWR_REGISTRY
"@
        exit 1
    }
}

function Build-Service {
    param([string]$ServiceName)
    
    $dockerfilePath = $SERVICES_MAP[$ServiceName]
    
    if (-not $dockerfilePath) {
        Write-Host "[ERROR] Unknown service: '$ServiceName'" -ForegroundColor Red
        Write-Host "Use -List to see available services"
        return $false
    }
    
    $fullImageName = "$SWR_REGISTRY/$SWR_ORGANIZATION/${ServiceName}:$IMAGE_TAG"
    
    Write-Host "================================================"
    Write-Host "Building image: $ServiceName"
    Write-Host "Dockerfile: $dockerfilePath"
    Write-Host "Image name: $fullImageName"
    Write-Host "================================================"
    
    Push-Location $PROJECT_ROOT
    try {
        # Use --platform linux/amd64 to ensure compatibility with x86_64 servers
        # Use --provenance=false and --sbom=false to avoid multi-platform manifest
        docker build `
            --platform linux/amd64 `
            --provenance=false `
            --sbom=false `
            -t $fullImageName `
            -f $dockerfilePath `
            .
        
        if ($LASTEXITCODE -ne 0) {
            throw "Image build failed"
        }
        
        Write-Host "[OK] Image built successfully: $fullImageName" -ForegroundColor Green
        return $true
    }
    finally {
        Pop-Location
    }
}

function Push-Service {
    param([string]$ServiceName)
    
    $fullImageName = "$SWR_REGISTRY/$SWR_ORGANIZATION/${ServiceName}:$IMAGE_TAG"
    
    Write-Host "================================================"
    Write-Host "Pushing image: $fullImageName"
    Write-Host "================================================"
    
    docker push $fullImageName
    
    if ($LASTEXITCODE -ne 0) {
        throw "Image push failed"
    }
    
    Write-Host "[OK] Image pushed successfully: $fullImageName" -ForegroundColor Green
}

function Build-AndPush-Service {
    param([string]$ServiceName)
    
    if ($BuildOnly) {
        Build-Service -ServiceName $ServiceName
    }
    elseif ($PushOnly) {
        Push-Service -ServiceName $ServiceName
    }
    else {
        $result = Build-Service -ServiceName $ServiceName
        if ($result) {
            Push-Service -ServiceName $ServiceName
        }
    }
}

# ============================================================
# Main Logic
# ============================================================

# Show help
if ($Help) {
    Show-Help
    exit 0
}

# List services
if ($List) {
    Show-ServiceList
    exit 0
}

# Execute login
if ($Login) {
    Invoke-SwrLogin
    if (-not $Services -and -not $All) {
        exit 0
    }
}

# Determine services to build
$servicesToBuild = @()
if ($All) {
    $servicesToBuild = $SERVICES_MAP.Keys | Sort-Object
}
elseif ($Services) {
    $servicesToBuild = $Services
}

# Check if any services need to be built
if ($servicesToBuild.Count -eq 0) {
    Write-Host "[ERROR] Please specify services to build, or use -All to build all services" -ForegroundColor Red
    Write-Host ""
    Show-Help
    exit 1
}

# Display build info
Write-Host "================================================"
Write-Host "SWR Repository Configuration"
Write-Host "================================================"
Write-Host "Registry:     $SWR_REGISTRY"
Write-Host "Organization: $SWR_ORGANIZATION"
Write-Host "Tag:          $IMAGE_TAG"
Write-Host "Services:     $($servicesToBuild -join ', ')"
Write-Host "================================================"
Write-Host ""

# Build and push each service
$successCount = 0
$failCount = 0

foreach ($service in $servicesToBuild) {
    try {
        Build-AndPush-Service -ServiceName $service
        $successCount++
        Write-Host ""
    }
    catch {
        Write-Host "[FAILED] Service $service failed: $_" -ForegroundColor Red
        $failCount++
    }
}

# Display results
Write-Host "================================================"
if ($failCount -eq 0) {
    Write-Host "[DONE] All operations completed! ($successCount services)" -ForegroundColor Green
}
else {
    Write-Host "[WARNING] Completed with $failCount failures (success: $successCount)" -ForegroundColor Yellow
}
Write-Host "================================================"
