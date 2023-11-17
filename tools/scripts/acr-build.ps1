<#
 .SYNOPSIS
    Push multi arch containers to Azure container registry

 .DESCRIPTION
    The script requires az to be installed and already logged on to a
    tenant. This means it should be run in a azcliv2 task in the
    azure pipeline or "az login" must have been performed already.

 .PARAMETER Registry
    The name of the registry
 .PARAMETER Subscription
    The subscription to use - otherwise uses default
 .PARAMETER ImageNamespace
    The namespace to use for the image inside the registry.
 .PARAMETER ImageTag
    Image tags to combine into manifest. Defaults to "latest"
 .PARAMETER PublishTags
    Comma seperated tags to publish. Defaults to Tag and latest

 .PARAMETER Debug
    Whether to build Release or Debug - default to Release.
 .PARAMETER NoBuid
    If set does not build but just packages the images into a
    manifest list
#>

Param(
    [string] $Registry = $null,
    [string] $Subscription = $null,
    [string] $ImageNamespace = $null,
    [string] $ImageTag = "latest",
    [string] $PublishTags = $null,
    [switch] $Debug,
    [switch] $NoBuild
)
$ErrorActionPreference = "Stop"

if (!$script:PublishTags) {
    $script:PublishTags = "latest"
    if ($script:ImageTag -ne "latest") {
        $script:PublishTags = "$($script:PublishTags),$($script:ImageTag)"
    }
}

if ([string]::IsNullOrEmpty($script:Registry)) {
    $script:Registry = $env.BUILD_REGISTRY
    if ([string]::IsNullOrEmpty($script:Registry)) {
        # Feature builds by default into dev registry
        $script:Registry = "industrialiotdev"
    }
}

if ([string]::IsNullOrEmpty($script:Subscription)) {
    $argumentList = @("account", "show")
    $account = & "az" @argumentList 2>$null | ConvertFrom-Json
    if (!$account) {
        throw "Failed to retrieve account information."
    }
    $script:Subscription = $account.name
    Write-Host "Using default subscription $script:Subscription..."
}

# get registry information
$argumentList = @("acr", "show", "--name", $script:Registry,
    "--subscription", $script:Subscription)
$script:RegistryInfo = (& "az" @argumentList 2>&1 | ForEach-Object { "$_" }) | ConvertFrom-Json
if ($LastExitCode -ne 0) {
    throw "az $($argumentList) failed with $($LastExitCode)."
}
$resourceGroup = $script:RegistryInfo.resourceGroup
Write-Debug "Using resource group $($resourceGroup)"
# get credentials
$argumentList = @("acr", "credential", "show", "--name", $script:Registry,
    "--subscription", $script:Subscription)
$credentials = (& "az" @argumentList 2>&1 | ForEach-Object { "$_" }) | ConvertFrom-Json
if ($LastExitCode -ne 0) {
    throw "az $($argumentList) failed with $($LastExitCode)."
}
$user = $credentials.username
$password = $credentials.passwords[0].value
Write-Debug "Using User name $($user) and passsword ****"

Write-Host "Build and push manifest lists to $($script:Registry).azurecr.io..."

# Build the manifest list from the images in the manifest
& (Join-Path $PSScriptRoot "build.ps1") -Registry "$($script:Registry).azurecr.io" `
    -User $user -Pw $password -PublishTags $script:PublishTags `
    -ImageNamespace $script:ImageNamespace -ImageTag $script:ImageTag `
    -NoBuild:$script:NoBuild -Debug:$script:Debug

if ($LastExitCode -ne 0) {
    throw "Failed to build and push manifest list."
}
Write-Host "Manifest lists were successfully pushed to $($script:Registry).azurecr.io."
