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
 .PARAMETER Debug
    Whether to build Release or Debug - default to Release.  
#>

Param(
    [string] $Registry = $null,
    [string] $Subscription = $null,
    [string] $ImageNamespace = $null,
    [switch] $Debug
)

if ([string]::IsNullOrEmpty($Registry)) {
    $Registry = $env.BUILD_REGISTRY
    if ([string]::IsNullOrEmpty($Registry)) {
        # Feature builds by default into dev registry
        $Registry = "industrialiotdev"
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

Write-Host "Build and push manifest lists..."
# Build the docker images and push them to acr
& (Join-Path $PSScriptRoot "manifest.ps1") -Registry "$($Registry).azurecr.io" `
    -Debug:$script:Debug -User $user -Pw $password `
    -ImageNamespace $script:ImageNamespace `
    -Push

Write-Host "Manifests pushed."
