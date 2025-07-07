<#
    .SYNOPSIS
        Auto onboard discovered assets and devices as assets and devices
        in Azure IoT Operations.
    .DESCRIPTION
        This script is used to auto onboard discovered assets and devices
        as assets and devices in Azure IoT Operations. It requires the
        Azure CLI and the Azure IoT Operations extension to be installed.
        The script will monitor (loop) for new discovered assets and
        devices in ADR and copy them as new assets and devices. This
        is a simpler way to test discovery than the UX based onboarding
        flow via the Digital Operation experience.
    .NOTES
        DO NOT USE FOR PRODUCTION SYSTEMS. This script is intended for
        development and testing purposes only.

    .PARAMETER AdrNamespace
        The ADR namespace to use for the instance.
        Default is the same as the instance name.
    .PARAMETER ResourceGroup
        The resource group to create or use for the cluster.
        Default is the same as the cluster name.
    .PARAMETER SubscriptionId
        The subscription id where the namespace is located.
        If not specified, the script will use the current subscription.
    .PARAMETER Location
        The location of the cluster.
        Default is "westus".
    .PARAMETER TenantId
        The tenant id to use when logging into Azure.
        If not specified, the script will use the current tenant.
    .PARAMETER RunOnce
        If specified, the script will run only once and exit.
#>

param(
    [string] [Parameter(Mandatory = $true)] $AdrNamespaceName,
    [string] [Parameter(Mandatory = $true)] $ResourceGroup,
    [string] $SubscriptionId,
    [string] $Location= "westus",
    [string] $TenantId,
    [switch] $RunOnce
)
$ErrorActionPreference = 'Continue'

$azVersion = (az version)[1].Split(":")[1].Split('"')[1]
if ($azVersion -lt "2.74.0" -or !$azVersion) {
    Write-Host "Azure CLI version 2.74.0 or higher is required." `
        -ForegroundColor Red
    exit -1
}

#
# Log into azure
#
Write-Host "Log into Azure..." -ForegroundColor Cyan
$loginParams = @( "--only-show-errors" )
if (![string]::IsNullOrWhiteSpace($TenantId)) {
    $loginParams += @("--tenant", $TenantId)
}
$session = (az login @loginParams) | ConvertFrom-Json
if (-not $session) {
    Write-Host "Error: Login failed." -ForegroundColor Red
    exit -1
}
if ([string]::IsNullOrWhiteSpace($SubscriptionId)) {
    $SubscriptionId = $session[0].id
}
if ([string]::IsNullOrWhiteSpace($TenantId)) {
    $TenantId = $session[0].tenantId
}

#
# Check adr namespace exists
#
$adrNsResource = "/subscriptions/$($SubscriptionId)"
$adrNsResource = "$($adrNsResource)/resourceGroups/$($ResourceGroup)"
$adrNsResource = "$($adrNsResource)/providers/Microsoft.DeviceRegistry"
$adrNsResource = "$($adrNsResource)/namespaces/$($AdrNamespaceName)"
$errOut = $($ns = & { az rest --method get `
    --url "$($adrNsResource)?api-version=2025-07-01-preview" `
    --headers "Content-Type=application/json" } | ConvertFrom-Json) 2>&1
if (!$ns -or !$ns.id) {
    Write-Host "ADR namespace $($adrNsResource) not found - $($errOut)." `
        -ForegroundColor Red
    exit -1
}

Write-Host "Onboarding devices and assets in ADR namespace $($ns.name)..." `
    -ForegroundColor Green
while ($true) {
    $errOut = $($dDevices = & { az rest --method get `
        --url "$($ns.id)/discoveredDevices?api-version=2025-07-01-preview" `
        --headers "Content-Type=application/json" } | ConvertFrom-Json) 2>&1
    if ($dDevices -and $dDevices.value) {
        foreach ($dDevice in $dDevices.value) {
            $body = $(@{
                extendedLocation = $dDevice.extendedLocation
                location = $dDevice.location
                properties = @{
                    # externalDeviceId = "unique-edge-device-identifier"
                    enabled = $true
                    endpoints = $dDevice.properties.endpoints
                }
            } | ConvertTo-Json -Depth 100 -Compress).Replace('"', '\"')
            Write-Host "Create or update device $($dDevice.name)..." -ForegroundColor Cyan
            $errOut = $($device = & { az rest --method put `
                --url "$($ns.id)/devices/$($dDevice.name)?api-version=2025-07-01-preview" `
                --headers "Content-Type=application/json" `
                --body $body } | ConvertFrom-Json) 2>&1
            if (!$device.id) {
                Write-Host "Error onboarding device $($dDevice.Name): $($errOut)" `
                    -ForegroundColor Red
            } else {
                Write-Host "Device $($device.id) created or updated successfully." `
                    -ForegroundColor Green
            }
        }
    }
    else {
        Write-Host "No discovered devices found." -ForegroundColor Yellow
    }
    $errOut = $($dAssets = & { az rest --method get `
        --url "$($ns.id)/discoveredAssets?api-version=2025-07-01-preview" `
        --headers "Content-Type=application/json" } | ConvertFrom-Json) 2>&1
    if ($dAssets -and $dAssets.value) {
        foreach ($dAsset in $dAssets.value) {
            # todo: Filter data points too
            $datasets = $dAsset.properties.datasets `
                | Select-Object -Property * -ExcludeProperty lastUpdatedOn
            # todo: Filter data points too
            $events = $dAsset.properties.events `
                | Select-Object -Property * -ExcludeProperty lastUpdatedOn
            $streams = $dAsset.properties.streams `
                | Select-Object -Property * -ExcludeProperty lastUpdatedOn
            $managementGroups = $dAsset.properties.managementGroups `
                | Select-Object -Property * -ExcludeProperty lastUpdatedOn

            $body = $(@{
                extendedLocation = $dAsset.extendedLocation
                location = $dAsset.location
                properties = @{
                    # externalAssetId = "unique-edge-device-identifier"
                    enabled = $true
                    displayName = $dAsset.properties.displayName
                    description = $dAsset.properties.description
                    manufacturer = $dAsset.properties.manufacturer
                    model = $dAsset.properties.model
                    productCode = $dAsset.properties.productCode
                    hardwareRevision = $dAsset.properties.hardwareRevision
                    softwareRevision = $dAsset.properties.softwareRevision
                    documentationUri = $dAsset.properties.documentationUri
                    serialNumber = $dAsset.properties.serialNumber
                    defaultDatasetsConfiguration = `
                        $dAsset.properties.defaultDatasetsConfiguration
                    defaultManagementGroupsConfiguration = `
                        $dAsset.properties.defaultManagementGroupsConfiguration
                    # .... add more properties as needed
                    deviceRef = $dAsset.properties.deviceRef
                    discoveredAssetRefs = @($dAsset.name)
                    assetTypeRefs = $dAsset.properties.assetTypeRefs
                    datasets = $datasets
                    events = $events
                    streams = $streams
                    managementGroups = $managementGroups
                }
            } | ConvertTo-Json -Depth 100 -Compress).Replace('"', '\"')
            Write-Host "Create or update asset $($dAsset.name)..." -ForegroundColor Cyan
            $errOut = $($asset = & { az rest --method put `
                --url "$($ns.id)/assets/$($dAsset.name)?api-version=2025-07-01-preview" `
                --headers "Content-Type=application/json" `
                --body $body } | ConvertFrom-Json) 2>&1
            if (!$asset.id) {
                Write-Host "Error onboarding asset $($dAsset.Name): $($errOut)" `
                    -ForegroundColor Red
            } else {
                Write-Host "Asset $($asset.id) created or updated successfully." `
                    -ForegroundColor Green
            }
        }
    }
    else {
        Write-Host "No discovered assets found." -ForegroundColor Yellow
    }
    if ($script:RunOnce.IsPresent) {
        break
    }
    Start-Sleep -Seconds 5
}
