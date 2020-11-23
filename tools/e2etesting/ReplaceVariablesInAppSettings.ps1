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
    [string] $ImageTag
)

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
    Write-Host "##vso[task.complete result=Failed]ClientId not set, exiting."
}

if (!$ClientSecret) {
    Write-Host "##vso[task.complete result=Failed]ClientSecret not set, exiting."
}

if (!$ApplicationName) {
    Write-Host "##vso[task.complete result=Failed]ApplicationName not set, exiting."
}

if (!$AppSettingsFilename) {
    Write-Host "##vso[task.complete result=Failed]AppSettingsFilename not set, exiting."
}

if (!$ResourceGroupName) {
    Write-Host "##vso[task.complete result=Failed]ResourceGroupName not set, exiting."
}

if (!$Region) {
    Write-Host "##vso[task.complete result=Failed]Region not set, exiting."
}

if (!$ImageTag) {
    Write-Host "##vso[task.complete result=Failed]ImageTag not set, exiting."
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

$fileContent | Out-File $AppSettingsFilename -Force -Encoding utf8