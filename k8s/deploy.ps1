#
# Go-Nomads Kubernetes Deployment Script (PowerShell)
# Usage: .\deploy.ps1 -Environment [env] -Action [action]
# Environment: dev, staging, prod (default: dev)
# Action: deploy, delete, status, build (default: deploy)
#

param(
    [string]$Environment = "dev",
    [string]$Action = "deploy"
)

# Script configuration
$ErrorActionPreference = "Continue"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$K8sDir = $ScriptDir

# Docker Registry configuration
$DockerRegistry = if ($env:DOCKER_REGISTRY) { $env:DOCKER_REGISTRY } else { "your-registry.com" }
$ImageTag = if ($env:IMAGE_TAG) { $env:IMAGE_TAG } else { "latest" }

# Output functions
function Write-Info {
    param([string]$Message)
    Write-Host "[INFO] $Message" -ForegroundColor Blue
}

function Write-Success {
    param([string]$Message)
    Write-Host "[SUCCESS] $Message" -ForegroundColor Green
}

function Write-Warn {
    param([string]$Message)
    Write-Host "[WARNING] $Message" -ForegroundColor Yellow
}

function Write-Err {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor Red
}

# Check kubectl
function Test-Kubectl {
    try {
        $null = kubectl version --client 2>&1
        Write-Success "kubectl is ready"
        return $true
    }
    catch {
        Write-Err "kubectl not installed"
        return $false
    }
}

# Check cluster connection
function Test-Cluster {
    try {
        $null = kubectl cluster-info 2>&1
        Write-Success "Connected to Kubernetes cluster"
        return $true
    }
    catch {
        Write-Err "Cannot connect to Kubernetes cluster"
        return $false
    }
}

# Process config file variables
function Get-ProcessedConfig {
    param([string]$FilePath)
    
    if (Test-Path $FilePath) {
        $content = Get-Content $FilePath -Raw
        $content = $content -replace '\$\{DOCKER_REGISTRY\}', $DockerRegistry
        $content = $content -replace '\$\{IMAGE_TAG\}', $ImageTag
        $content = $content -replace '\$\{IMAGE_TAG:-latest\}', $ImageTag
        return $content
    }
    return $null
}

# Apply config file
function Apply-Config {
    param(
        [string]$FilePath,
        [string]$Description
    )
    
    Write-Info "Deploying: $Description"
    
    if (Test-Path $FilePath) {
        if ($FilePath -match "service|gateway") {
            $processedContent = Get-ProcessedConfig -FilePath $FilePath
            $processedContent | kubectl apply -f -
        }
        else {
            kubectl apply -f $FilePath
        }
        Write-Success "Deployed: $Description"
    }
    else {
        Write-Warn "File not found: $FilePath"
    }
}

# Wait for deployment
function Wait-ForDeployment {
    param(
        [string]$DeploymentName,
        [string]$Namespace,
        [int]$TimeoutSeconds = 300
    )
    
    Write-Info "Waiting for $DeploymentName..."
    kubectl rollout status deployment/$DeploymentName -n $Namespace --timeout="${TimeoutSeconds}s"
}

# Deploy base config
function Deploy-Base {
    Write-Info "========== Deploying Base Config =========="
    
    Apply-Config -FilePath "$K8sDir\base\namespace.yaml" -Description "Namespace"
    Apply-Config -FilePath "$K8sDir\base\configmap.yaml" -Description "ConfigMap"
    Apply-Config -FilePath "$K8sDir\base\secrets.yaml" -Description "Secrets"
    
    Write-Success "Base config deployed"
}

# Deploy infrastructure
function Deploy-Infrastructure {
    Write-Info "========== Deploying Infrastructure =========="
    
    Apply-Config -FilePath "$K8sDir\infrastructure\redis.yaml" -Description "Redis"
    Apply-Config -FilePath "$K8sDir\infrastructure\rabbitmq.yaml" -Description "RabbitMQ"
    Apply-Config -FilePath "$K8sDir\infrastructure\elasticsearch.yaml" -Description "Elasticsearch"
    
    Write-Info "Waiting for infrastructure..."
    Start-Sleep -Seconds 10
    
    Wait-ForDeployment -DeploymentName "redis" -Namespace "go-nomads" -TimeoutSeconds 120
    Wait-ForDeployment -DeploymentName "rabbitmq" -Namespace "go-nomads" -TimeoutSeconds 180
    
    Write-Success "Infrastructure deployed"
}

# Deploy services
function Deploy-Services {
    Write-Info "========== Deploying Services =========="
    
    Apply-Config -FilePath "$K8sDir\services\gateway.yaml" -Description "API Gateway"
    Apply-Config -FilePath "$K8sDir\services\user-service.yaml" -Description "User Service"
    Apply-Config -FilePath "$K8sDir\services\city-service.yaml" -Description "City Service"
    Apply-Config -FilePath "$K8sDir\services\coworking-service.yaml" -Description "Coworking Service"
    Apply-Config -FilePath "$K8sDir\services\event-service.yaml" -Description "Event Service"
    Apply-Config -FilePath "$K8sDir\services\ai-service.yaml" -Description "AI Service"
    Apply-Config -FilePath "$K8sDir\services\message-service.yaml" -Description "Message Service"
    Apply-Config -FilePath "$K8sDir\services\cache-service.yaml" -Description "Cache Service"
    
    Write-Success "Services deployed"
}

# Full deployment
function Deploy-All {
    Write-Info "Starting full deployment of Go-Nomads..."
    Write-Info "Environment: $Environment"
    Write-Info "Docker Registry: $DockerRegistry"
    Write-Info "Image Tag: $ImageTag"
    Write-Host ""
    
    # 1. Deploy base config
    Deploy-Base
    Write-Host ""
    
    # 2. Deploy infrastructure
    Deploy-Infrastructure
    Write-Host ""
    
    # 3. Deploy monitoring
    Deploy-Monitoring
    Write-Host ""
    
    # 4. Deploy services
    Deploy-Services
    Write-Host ""
    
    Write-Success "========== Deployment Complete =========="
    Write-Info "Check pods: kubectl get pods -n go-nomads"
    Write-Info "Check services: kubectl get services -n go-nomads"
}

# Remove all resources
function Remove-All {
    Write-Warn "About to delete all resources in go-nomads namespace..."
    $confirm = Read-Host "Continue? (y/N)"
    
    if (($confirm -eq "y") -or ($confirm -eq "Y")) {
        kubectl delete namespace go-nomads --ignore-not-found
        Write-Success "Resources deleted"
    }
    else {
        Write-Info "Operation cancelled"
    }
}

# Show status
function Show-Status {
    Write-Info "========== Go-Nomads Status =========="
    Write-Host ""
    
    Write-Info "Pods:"
    kubectl get pods -n go-nomads -o wide
    Write-Host ""
    
    Write-Info "Services:"
    kubectl get services -n go-nomads
    Write-Host ""
    
    Write-Info "Deployments:"
    kubectl get deployments -n go-nomads
    Write-Host ""
    
    Write-Info "PersistentVolumeClaims:"
    kubectl get pvc -n go-nomads
    Write-Host ""
    
    Write-Info "Ingress:"
    kubectl get ingress -n go-nomads
}

# Build and push Docker images
function Build-AndPush {
    Write-Info "========== Building Docker Images =========="
    
    $ProjectRoot = Split-Path -Parent $K8sDir
    
    $services = @(
        @{Name = "gateway"; Dockerfile = "src/Gateway/Gateway/Dockerfile" }
        @{Name = "user-service"; Dockerfile = "src/Services/UserService/UserService/Dockerfile" }
        @{Name = "city-service"; Dockerfile = "src/Services/CityService/CityService/Dockerfile" }
        @{Name = "coworking-service"; Dockerfile = "src/Services/CoworkingService/CoworkingService/Dockerfile" }
        @{Name = "event-service"; Dockerfile = "src/Services/EventService/EventService/Dockerfile" }
        @{Name = "ai-service"; Dockerfile = "src/Services/AIService/AIService/Dockerfile" }
        @{Name = "message-service"; Dockerfile = "src/Services/MessageService/MessageService/API/Dockerfile" }
        @{Name = "cache-service"; Dockerfile = "src/Services/CacheService/CacheService/Dockerfile" }
    )
    
    foreach ($service in $services) {
        Write-Info "Building $($service.Name)..."
        
        $imageName = "$DockerRegistry/go-nomads-$($service.Name):$ImageTag"
        $dockerfilePath = Join-Path $ProjectRoot $service.Dockerfile
        
        docker build -t $imageName -f $dockerfilePath $ProjectRoot
        
        Write-Info "Pushing $($service.Name)..."
        docker push $imageName
        
        Write-Success "$($service.Name) built and pushed"
    }
    
    Write-Success "All images built and pushed"
}

# Main function
function Main {
    Write-Host ""
    Write-Host "================================"
    Write-Host "  Go-Nomads Kubernetes Deployer"
    Write-Host "================================"
    Write-Host ""
    
    if (-not (Test-Kubectl)) { exit 1 }
    if (-not (Test-Cluster)) { exit 1 }
    
    switch ($Action) {
        "deploy" { 
            Deploy-All 
        }
        "delete" { 
            Remove-All 
        }
        "status" { 
            Show-Status 
        }
        "build" { 
            Build-AndPush 
        }
        "infrastructure" { 
            Deploy-Base
            Deploy-Infrastructure 
        }
        "services" { 
            Deploy-Services 
        }
        "monitoring" { 
            Deploy-Monitoring 
        }
        default {
            Write-Err "Unknown action: $Action"
            Write-Host "Available: deploy, delete, status, build, infrastructure, services, monitoring"
            exit 1
        }
    }
}

# Execute main function
Main
