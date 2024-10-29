
<#
   .SYNOPSIS
      Setup Kind and connect cluster to Arc (must run as admin)
   .DESCRIPTION
      Setup Kind and connect cluster to Arc. This script will install
      kind required dependencies and connect it to the cloud via Arc.
   .NOTES
      DO NOT USE FOR PRODUCTION SYSTEMS. This script is intended for
      development and testing purposes only.

   .PARAMETER Name
      The name of the cluster
   .PARAMETER TenantId
      The tenant id to use when logging into Azure.
   .PARAMETER SubscriptionId
      The subscription id to scope all activity to.
   .PARAMETER Location
      The location of the cluster.
   .PARAMETER Force
      Force reinstall.
#>

param(
    [string] [Parameter(Mandatory = $true)] $Name,
    [string] $ResourceGroup,
    [string] $TenantId = "6e54c408-5edd-4f87-b3bb-360788b7ca18",
    [string] $SubscriptionId,
    [string] $Location,
    [switch] $Force
)

#Requires -RunAsAdministrator

$ErrorActionPreference = 'Stop'
$path = Split-Path $script:MyInvocation.MyCommand.Path

if (! [Environment]::Is64BitProcess) {
   Write-Host "Error: Run this in 64bit Powershell session" -ForegroundColor Red
   exit -1
}
if ([string]::IsNullOrWhiteSpace($ResourceGroup)) {
   $ResourceGroup = $Name
}
if ([string]::IsNullOrWhiteSpace($TenantId)) {
   $TenantId = $env:AZURE_TENANT_ID
}
if ([string]::IsNullOrWhiteSpace($Location)) {
   $Location = "eastus"
}

Write-Host "Ensuring all required dependencies are installed..." -ForegroundColor Cyan
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$errOut = $($docker = & {docker version --format json | ConvertFrom-Json}) 2>&1
if ($LASTEXITCODE -ne 0 -or !$docker.Server) {
    $docker | Out-Host
    throw "Docker not installed or running : $errOut"
}
Write-Host "Found $($docker.Server.Platform.Name)..." -ForegroundColor Green
$installAz = $false
try {
    $azVersion = (az version)[1].Split(":")[1].Split('"')[1]
    if ($azVersion -lt "2.61.0" -or !$azVersion) {
        $installAz = $true
    }
}
catch {
    $installAz = $true
}

# install az
if ($installAz) {
    Write-Host "Installing Az CLI..." -ForegroundColor Cyan

    Set-ExecutionPolicy Bypass -Scope Process -Force
    $ProgressPreference = 'SilentlyContinue'
    Invoke-WebRequest -Uri https://aka.ms/installazurecliwindowsx64 -OutFile .\AzureCLI.msi
    Start-Process msiexec.exe -Wait -ArgumentList '/I AzureCLI.msi /quiet'
    Remove-Item .\AzureCLI.msi
}

# install required az extensions
$extensions  =
@(
    "connectedk8s",
    "k8s-configuration"
)
foreach($p in $extensions) {
    $errOut = $($stdout = & {az extension add --name $p}) 2>&1
    if ($LASTEXITCODE -ne 0) {
        $stdout | Out-Host
        throw "Error installing az extension $p : $errOut"
    }
}

# install choco
$errout = $($version = & {choco --version}) 2>&1
if (!$version -or $errout) {
    Write-Host "Installing Choco CLI..." -ForegroundColor Cyan
    [System.Net.ServicePointManager]::SecurityProtocol = `
        [System.Net.ServicePointManager]::SecurityProtocol -bor 3072
    $scriptLoc = 'https://community.chocolatey.org/install.ps1'
    Invoke-Expression ((New-Object System.Net.WebClient).DownloadString($scriptLoc))
}

# install kind, helm and kubectl
$packages  =
@(
    "kubernetes-cli",
    "kubernetes-helm",
    "kind"
)
foreach($p in $packages) {
    $errOut = $($stdout = & {choco install $p --yes}) 2>&1
    if ($LASTEXITCODE -ne 0) {
        $stdout | Out-Host
        throw "Error choco installing package $p : $errOut"
    }
}

Write-Host "Log into Azure..." -ForegroundColor Cyan
$loginparams = @()
if (![string]::IsNullOrWhiteSpace($TenantId)) {
   $loginparams += @("-t", $TenantId)
}
if (![string]::IsNullOrWhiteSpace($SubscriptionId)) {
   $loginparams += @("-s", $SubscriptionId)
}
$session = (az login @loginparams) | ConvertFrom-Json
if (-not $session) {
    Write-Host "Error: Login failed." -ForegroundColor Red
    exit -1
}

$SubscriptionId = $session.id
$TenantId = $session.tenantId
Write-Host "Registering the required resource providers..." -ForegroundColor Cyan
$resourceProviders =
@(
    "Microsoft.ExtendedLocation",
    "Microsoft.Kubernetes",
    "Microsoft.KubernetesConfiguration"
)
foreach ($rp in $resourceProviders) {
    $errOut = $($obj = & {az provider show -n $rp `
        --subscription $SubscriptionId | ConvertFrom-Json}) 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Error querying provider $rp : $errOut"
    }
    if ($obj.registrationState -eq "Registered") {
        continue
    }
    $errOut = $($retVal = & {az provider register -n `
        $rp --subscription $SubscriptionId}) 2>&1
    if ($LASTEXITCODE -ne 0) {
        $retVal | Out-Host
        throw "Error registering provider $rp : $errOut"
    }
}

Write-Host "Creating resource group..." -ForegroundColor Cyan
$rg = (az group create --name $Name -l $Location `
    --subscription $SubscriptionId) | ConvertFrom-Json

Write-Host "Creating service principal..." -ForegroundColor Cyan
$sp = (az ad sp create-for-rbac -n "$Name" --role "Contributor" `
    --scopes /subscriptions/$SubscriptionId) | ConvertFrom-Json
if (!$sp.appId -or !$sp.password) {
    throw "Error creating service principal"
}

Write-Host "Creating cluster..." -ForegroundColor Cyan
kind delete cluster --name $Name | Out-Null
kind create cluster --config kind_cluster.yaml --name $Name
if ($LASTEXITCODE -ne 0) {
    throw "Error creating kind cluster"
}
Write-Host "Cluster created..." -ForegroundColor Green
kubectl get nodes

Write-Host "Connecting cluster to Arc in $($rg.Name)..." -ForegroundColor Cyan
az login --service-principal -u $sp.appId -p $sp.password --tenant $sp.tenant
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Login with service principal failed." -ForegroundColor Red
    exit -1
}

az connectedk8s connect -n $Name -g $Name --subscription $SubscriptionId `
    --correlation-id "d009f5dd-dba8-4ac7-bac9-b54ef3a6671a"

# delete
#az group delete --name $Name
#kind delete cluster --name $Name