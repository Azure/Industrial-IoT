<#
 .SYNOPSIS
    Replace template variables in appSettings.json for IAI.

 .DESCRIPTION
    This script replaces template variables in appSettings.json that will be used for deployment of Industrial IoT
    platform using IAI.
#>

Param(
    [Guid] $TenantId,
    [Guid] $SubscriptionId,
    [Guid] $ClientId,
    [string] $ClientSecret,
    [string] $ApplicationName,
    [string] $AppSettingsFilename,
    [string] $ResourceGroupName,
    [string] $Region,
    [string] $ImageTag,
    [string] $ImageNamespace,
    [string] $ContainerRegistryServer,
    [string] $ContainerRegistryUsername,
    [string] $ContainerRegistryPassword
)

# Stop execution when an error occurs.
$ErrorActionPreference = "Stop"

if (!$TenantId -or !$SubscriptionId) {
    Write-Host "Getting Azure Context..."
    $context = Get-AzContext

    if (!$SubscriptionId) {
        $SubscriptionId = $context.Subscription.Id
        Write-Host "Using Subscription Id $($subscriptionId)."
    }

    if (!$TenantId) {
        $TenantId = $context.Tenant.Id
        Write-Host "Using TenantId $($TenantId)."
    }
}

if (!$ClientId) {
   Write-Error "ClientId not set."
}

if (!$ClientSecret) {
   Write-Error "ClientSecret not set."
}

if (!$ApplicationName) {
   Write-Error "ApplicationName not set."
}

if (!$AppSettingsFilename) {
   Write-Error "AppSettingsFilename not set."
}

if (!$ResourceGroupName) {
   Write-Error "ResourceGroupName not set."
}

if (!$Region) {
   Write-Error "Region not set."
}

if (!$ImageTag) {
   Write-Error "ImageTag not set."
}

if (!$ImageNamespace) {
   $ImageNamespace = ""
}

if (!$ContainerRegistryServer) {
   $ContainerRegistryServer = "mcr.microsoft.com"
}

Write-Host "##[group]Parameter values"
Write-Host "TenantId: $($TenantId)"
Write-Host "SubscriptionId: $($SubscriptionId)"
Write-Host "ClientId: $($ClientId)"
Write-Host "ClientSecret: $($ClientSecret)"
Write-Host "ApplicationName: $($ApplicationName)"
Write-Host "ResourceGroupName: $($ResourceGroupName)"
Write-Host "Region: $($Region)"
Write-Host "ImageTag: $($ImageTag)"
Write-Host "ImageNamespace: $($ImageNamespace)"
Write-Host "ContainerRegistryServer: $($ContainerRegistryServer)"
Write-Host "ContainerRegistryUsername: $($ContainerRegistryUsername)"
Write-Host "AppSettingsFilename: $($AppSettingsFilename)"
Write-Host "##[endgroup]"

$fileContent = Get-Content $AppSettingsFilename -Raw

$fileContent = $fileContent -replace "{{TenantId}}", $TenantId
$fileContent = $fileContent -replace "{{SubscriptionId}}", $SubscriptionId
$fileContent = $fileContent -replace "{{ClientId}}", $ClientId
$fileContent = $fileContent -replace "{{ClientSecret}}", $ClientSecret
$fileContent = $fileContent -replace "{{ApplicationName}}", $ApplicationName
$fileContent = $fileContent -replace "{{ResourceGroupName}}", $ResourceGroupName
$fileContent = $fileContent -replace "{{Region}}", $Region
$fileContent = $fileContent -replace "{{ImageTag}}", $ImageTag
$fileContent = $fileContent -replace "{{ImageNamespace}}", $ImageNamespace
$fileContent = $fileContent -replace "{{ContainerRegistryServer}}", $ContainerRegistryServer
$fileContent = $fileContent -replace "{{ContainerRegistryUsername}}", $ContainerRegistryUsername
$fileContent = $fileContent -replace "{{ContainerRegistryPassword}}", $ContainerRegistryPassword
$fileContent | Out-File $AppSettingsFilename -Force -Encoding utf8
$fileContent | Out-Host

