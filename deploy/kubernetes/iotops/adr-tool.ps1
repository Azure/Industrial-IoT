<#
    .SYNOPSIS
        Enables onboarding and cleanup of namespace resources in
        Azure Device Registry.
    .DESCRIPTION
        This tool can be used to onboard discovered assets and devices
        as assets and devices or clean them up. It requires the Azure
        CLI to be installed. When used in "Onboard" mode, it will
        monitor (loop) for new discovered assets and devices in ADR
        and copy them as new assets and devices. This is a simpler
        way to test discovery than the UX based onboarding flow via
        the Digital Operation experience. When used in "Cleanup"
        mode, it will delete all discovered and onboarded assets and
        devices in ADR.
    .NOTES
        DO NOT USE FOR PRODUCTION SYSTEMS. This script is intended for
        development and testing purposes only.

    .PARAMETER Action
        The action to perform. Valid values are "Onboard" and "Cleanup".
        Default is "Onboard". If "Cleanup" is specified, the script will
        delete all discovered assets and devices.
    .PARAMETER AdrNamespaceName
        The ADR namespace to use for the instance.
        Default is the same as the instance name.
    .PARAMETER ResourceGroup
        The resource group to create or use for the cluster.
        Default is the same as the cluster name.
    .PARAMETER SubscriptionId
        The subscription id where the namespace is located.
        If not specified, the script will use the current subscription.
    .PARAMETER TenantId
        The tenant id to use when logging into Azure.
        If not specified, the script will use the current tenant.
    .PARAMETER RunOnce
        If specified, the script will run only one iteration of the
        action and exit. Otherwise it will run the action to completion.
    .PARAMETER Force
        If specified, the script will force the onboarding of devices
        and assets even if they already exist.
    .PARAMETER SkipLogin
        If specified, the script will skip the login step and use
        the current session.
#>

param(
    [string] [Parameter(Mandatory = $true)] [ValidateSet(
        "Onboard",
        "Cleanup"
    )] [string] $Action,
    [string] [Parameter(Mandatory = $true)] $Name,
    [string] $AdrNamespaceName,
    [string] $ResourceGroup,
    [string] $SubscriptionId = "53d910a7-f1f8-4b7a-8ee0-6e6b67bddd82",
    [string] $TenantId,
    [switch] $RunOnce,
    [switch] $Force,
    [switch] $SkipLogin
)

$ErrorActionPreference = 'Continue'

if ([string]::IsNullOrWhiteSpace($ResourceGroup)) {
    $ResourceGroup = $script:Name
    Write-Host "Using resource group $ResourceGroup..." -ForegroundColor Cyan
}
if ([string]::IsNullOrWhiteSpace($script:AdrNamespaceName)) {
    $script:AdrNamespaceName = $script:Name
    Write-Host "Using ADR namespace name $($script:AdrNamespaceName)..." -ForegroundColor Cyan
}

$azVersion = (az version)[1].Split(":")[1].Split('"')[1]
if ($azVersion -lt "2.74.0" -or !$azVersion) {
    Write-Host "Azure CLI version 2.74.0 or higher is required." `
        -ForegroundColor Red
    exit -1
}

#
# Log into azure
#
if (-not $script:SkipLogin.IsPresent) {
    Write-Host "Log into Azure..." -ForegroundColor Cyan
    $loginParams = @( "--only-show-errors" )
    if (![string]::IsNullOrWhiteSpace($script:TenantId)) {
        $loginParams += @("--tenant", $script:TenantId)
    }
    $session = (az login @loginParams) | ConvertFrom-Json
    if (-not $session) {
        Write-Host "Error: Login failed." -ForegroundColor Red
        exit -1
    }
    if ([string]::IsNullOrWhiteSpace($script:SubscriptionId)) {
        $script:SubscriptionId = $session[0].id
    }
    if ([string]::IsNullOrWhiteSpace($script:TenantId)) {
        $script:TenantId = $session[0].tenantId
    }
}

#
# Check adr namespace exists
#
$adrNsResource = "/subscriptions/$($script:SubscriptionId)"
$adrNsResource = "$($adrNsResource)/resourceGroups/$($script:ResourceGroup)"
$adrNsResource = "$($adrNsResource)/providers/Microsoft.DeviceRegistry"
$adrNsResource = "$($adrNsResource)/namespaces/$($AdrNamespaceName)"
$errOut = $($ns = & { az rest --method get `
    --url "$($adrNsResource)?api-version=2025-10-01" `
    --headers "Content-Type=application/json" } | ConvertFrom-Json) 2>&1
if (!$ns -or !$ns.id) {
    Write-Host "ADR namespace $($adrNsResource) not found - $($errOut)." `
        -ForegroundColor Red
    exit -1
}

#
# Remove properties from discovered resource that are not supported on actual resource
#
function Remove-PropertyRecursively {
    param (
        [Parameter(Mandatory)] [PSObject] $Object,
        [Parameter(Mandatory)] [string] $PropertyName
    )

    # Check if the object is a hashtable or PSCustomObject
    if ($Object -is [System.Collections.IDictionary]) {
        # Remove the property if it exists
        if ($Object.ContainsKey($PropertyName)) {
            $Object.Remove($PropertyName)
        }

        # Recursively process nested objects
        foreach ($key in $Object.Keys) {
            Remove-PropertyRecursively -Object $Object[$key] -PropertyName $PropertyName
        }
    } elseif ($Object -is [System.Collections.IEnumerable] -and -not ($Object -is [string])) {
        # Iterate through enumerable objects
        foreach ($item in $Object) {
            Remove-PropertyRecursively -Object $item -PropertyName $PropertyName
        }
    } elseif ($Object -is [PSCustomObject]) {
        # Remove the property if it exists
        if ($Object.PSObject.Properties[$PropertyName]) {
            $Object.PSObject.Properties.Remove($PropertyName)
        }

        # Recursively process nested properties
        foreach ($property in $Object.PSObject.Properties) {
            Remove-PropertyRecursively -Object $property.Value -PropertyName $PropertyName
        }
    }
}

if ($script:Action -eq "Cleanup") {
    $resourceTypes = @(
        "discoveredAssets",
        "discoveredDevices",
        "assets",
        "devices"
    )
    $itemsDeleted = $true
    while ($itemsDeleted) {
        $itemsDeleted = $false
        foreach ($resourceType in $resourceTypes) {
            Write-Host "Deleting all $($resourceType) in ADR namespace $($ns.name)..." `
                -ForegroundColor Cyan
            $errOut = $($resources = & { az rest --method get `
                --url "$($ns.id)/$($resourceType)?api-version=2025-10-01" `
                --headers "Content-Type=application/json" } | ConvertFrom-Json) 2>&1
            if ($resources -and $resources.value) {
                foreach ($resource in $resources.value) {
                    $itemsDeleted = $true
                    $errOut = & { az rest --method delete `
                        --url "$($resource.id)?api-version=2025-10-01" `
                        --headers "Content-Type=application/json" } 2>&1
                    $name = "$($resourceType.TrimEnd('s')) $($resource.name)"
                    if (-not $?) {
                        Write-Host "Error deleting $($name): $($errOut)" `
                            -ForegroundColor Red
                    }
                    else {
                        Write-Host "Deleted $($name)." -ForegroundColor Green
                    }
                }
            }
        }
        if ($script:RunOnce.IsPresent) {
            break
        }
    }
}
elseif ($script:Action -eq "Onboard") {
    $tempFile = New-TemporaryFile
    Write-Host "Onboarding devices and assets in ADR namespace $($ns.name)..." `
        -ForegroundColor Green
    while ($true) {
        $errOut = $($dDevices = & { az rest --method get `
            --url "$($ns.id)/discoveredDevices?api-version=2025-10-01" `
            --headers "Content-Type=application/json" } | ConvertFrom-Json) 2>&1
        $onboardComplete = $false
        $needsSync = $false
        if ($dDevices -and $dDevices.value) {
            foreach ($dDevice in $dDevices.value) {

                $errOut = $($device = & { az rest --method get `
                    --url "$($ns.id)/devices/$($dDevice.name)?api-version=2025-10-01" `
                    --headers "Content-Type=application/json" } | ConvertFrom-Json) 2>&1
                if ($device -and $device.id) {
                    Write-Host "Device $($device.name) exists with version $($device.properties.version)..." `
                        -ForegroundColor Cyan
                    if ($dDevice.properties.version -eq 1) { # always sync first version
                        $needsSync = $true
                    }
                    elseif ($device.properties.version -ne $dDevice.properties.version) {
                        Write-Host "Discovered Device $($dDevice.name) has a different version $($dDevice.properties.version)." `
                            -ForegroundColor Yellow
                        $needsSync = $true
                    }
                    elseif (-not $script:Force.IsPresent) {
                        Write-Host "Device $($device.name) is up to date." -ForegroundColor Green
                        $onboardComplete = $true
                        continue
                    }
                }

                Remove-PropertyRecursively -Object $dDevice.properties -PropertyName "supportedAuthenticationMethods"
                $body = @{
                    extendedLocation = $dDevice.extendedLocation
                    location = $dDevice.location
                    properties = @{
                        externalDeviceId = $dDevice.properties.externalDeviceId
                        enabled = $true
                        endpoints = $dDevice.properties.endpoints
                    }
                } | ConvertTo-Json -Depth 100
                #$body | Out-Host
                $body | Out-File -FilePath $tempFile -Encoding utf8 -Force
                Write-Host "Create or update device $($dDevice.name)..." -ForegroundColor Cyan
                $errOut = $($device = & { az rest --method put `
                    --url "$($ns.id)/devices/$($dDevice.name)?api-version=2025-10-01" `
                    --headers "Content-Type=application/json" `
                    --body @$tempFile } | ConvertFrom-Json) 2>&1
                if (!$device.id) {
                    Write-Host "Error onboarding device $($dDevice.name): $($errOut)" `
                        -ForegroundColor Red
                } else {
                    Write-Host "Device $($device.name) created or updated successfully." `
                        -ForegroundColor Green
                    $onboardComplete = $true
                }
            }
        }
        else {
            Write-Host "No discovered devices found." -ForegroundColor Yellow
        }
        $errOut = $($dAssets = & { az rest --method get `
            --url "$($ns.id)/discoveredAssets?api-version=2025-10-01" `
            --headers "Content-Type=application/json" } | ConvertFrom-Json) 2>&1
        if ($dAssets -and $dAssets.value) {
            foreach ($dAsset in $dAssets.value) {
                $errOut = $($asset = & { az rest --method get `
                    --url "$($ns.id)/assets/$($dAsset.name)?api-version=2025-10-01" `
                    --headers "Content-Type=application/json" } | ConvertFrom-Json) 2>&1
                if ($asset -and $asset.id) {
                    Write-Host "Asset $($asset.name) exists with version $($asset.properties.version)..." `
                        -ForegroundColor Cyan
                    if ($asset.properties.version -ne $dAsset.properties.version) {
                        Write-Host "Discovered Asset $($dAsset.name) has a different version $($dAsset.properties.version)." `
                            -ForegroundColor Yellow
                        $needsSync = $true
                    }
                    elseif (-not $script:Force.IsPresent) {
                        Write-Host "Asset $($asset.name) is up to date." -ForegroundColor Green
                        $onboardComplete = $true
                        continue
                    }
                }

                $displayName = $dAsset.properties.displayName
                if (!$displayName) {
                    $displayName = $dAsset.properties.model
                }
                Remove-PropertyRecursively -Object $dAsset.properties -PropertyName "lastUpdatedOn"
                $body = @{
                    extendedLocation = $dAsset.extendedLocation
                    location = $dAsset.location
                    properties = @{
                        externalAssetId = $dAsset.properties.externalAssetId
                        enabled = $true
                        displayName = $displayName
                        description = $dAsset.properties.description
                        manufacturer = $dAsset.properties.manufacturer
                        model = $dAsset.properties.model
                        productCode = $dAsset.properties.productCode
                        hardwareRevision = $dAsset.properties.hardwareRevision
                        softwareRevision = $dAsset.properties.softwareRevision
                        documentationUri = $dAsset.properties.documentationUri
                        serialNumber = $dAsset.properties.serialNumber
                        defaultDatasetsDestinations = `
                            $dAsset.properties.defaultDatasetsDestinations
                        defaultEventsDestinations = `
                            $dAsset.properties.defaultEventsDestinations
                        defaultStreamsDestinations = `
                            $dAsset.properties.defaultStreamsDestinations
                        defaultDatasetsConfiguration = `
                            $dAsset.properties.defaultDatasetsConfiguration
                        defaultEventsConfiguration = `
                            $dAsset.properties.defaultEventsConfiguration
                        defaultStreamsConfiguration = `
                            $dAsset.properties.defaultStreamsConfiguration
                        defaultManagementGroupsConfiguration = `
                            $dAsset.properties.defaultManagementGroupsConfiguration
                        # .... add more properties as needed
                        deviceRef = $dAsset.properties.deviceRef
                        discoveredAssetRefs = @($dAsset.name)
                        assetTypeRefs = [array]$dAsset.properties.assetTypeRefs
                        datasets = [array]$dAsset.properties.datasets
                        events = [array]$dAsset.properties.events
                        streams = [array]$dAsset.properties.streams
                        managementGroups = [array]$dAsset.properties.managementGroups
                    }
                } | ConvertTo-Json -Depth 100
                #$body | Out-Host
                $body | Out-File -FilePath $tempFile -Encoding utf8 -Force
                Write-Host "Create or update asset $($dAsset.name)..." -ForegroundColor Cyan
                $errOut = $($asset = & { az rest --method put `
                    --url "$($ns.id)/assets/$($dAsset.name)?api-version=2025-10-01" `
                    --headers "Content-Type=application/json" `
                    --body @$tempFile } | ConvertFrom-Json) 2>&1
                if (!$asset.id) {
                    Write-Host "Error onboarding asset $($dAsset.name): $($errOut)" `
                        -ForegroundColor Red
                } else {
                    Write-Host "Asset $($asset.name) created or updated successfully." `
                        -ForegroundColor Green
                    $onboardComplete = $true
                }
            }
        }
        else {
            Write-Host "No discovered assets found." -ForegroundColor Yellow
        }
        if ($script:RunOnce.IsPresent -and $onboardComplete -and -not $needsSync) {
            break
        }
        if ($needsSync) {
            continue
        }
        Start-Sleep -Seconds 60
    }
    Remove-Item -Path $tempFile -Force
}
else {
    Write-Host "Invalid action $($script:Action)." -ForegroundColor Red
    exit -1
}
