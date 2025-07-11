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

    .PARAMETER AdrNamespaceName
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
    [string] $Location = "westus",
    [string] $TenantId,
    [switch] $RunOnce,
    [switch] $SkipLogin
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
if (-not $script:SkipLogin.IsPresent) {
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

$tempFile = New-TemporaryFile
Write-Host "Onboarding devices and assets in ADR namespace $($ns.name)..." `
    -ForegroundColor Green
while ($true) {
    $errOut = $($dDevices = & { az rest --method get `
        --url "$($ns.id)/discoveredDevices?api-version=2025-07-01-preview" `
        --headers "Content-Type=application/json" } | ConvertFrom-Json) 2>&1
    $onboardComplete = $false
    $needsSync = $false
    if ($dDevices -and $dDevices.value) {
        foreach ($dDevice in $dDevices.value) {

            $device = & { az rest --method get `
                --url "$($ns.id)/devices/$($dDevice.name)?api-version=2025-07-01-preview" `
                --headers "Content-Type=application/json" } | ConvertFrom-Json
            if ($device -and $device.id) {
                Write-Host "Device $($device.name) exists with version $($device.properties.version)..." `
                    -ForegroundColor Cyan
                if ($device.properties.version -ne $dDevice.properties.version) {
                    Write-Host "Discovered Device $($dDevice.name) has a different version $($dDevice.properties.version)." `
                        -ForegroundColor Yellow
                    $needsSync = $true
                }
                else {
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
                    endpoints = $endpoints
                }
            } | ConvertTo-Json -Depth 100
            #$body | Out-Host
            $body | Out-File -FilePath $tempFile -Encoding utf8 -Force
            Write-Host "Create or update device $($dDevice.name)..." -ForegroundColor Cyan
            $errOut = $($device = & { az rest --method put `
                --url "$($ns.id)/devices/$($dDevice.name)?api-version=2025-07-01-preview" `
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
        --url "$($ns.id)/discoveredAssets?api-version=2025-07-01-preview" `
        --headers "Content-Type=application/json" } | ConvertFrom-Json) 2>&1
    if ($dAssets -and $dAssets.value) {
        foreach ($dAsset in $dAssets.value) {
            $asset = & { az rest --method get `
                --url "$($ns.id)/assets/$($dAsset.name)?api-version=2025-07-01-preview" `
                --headers "Content-Type=application/json" } | ConvertFrom-Json
            if ($asset -and $asset.id) {
                Write-Host "Asset $($asset.name) exists with version $($asset.properties.version)..." `
                    -ForegroundColor Cyan
                if ($asset.properties.version -ne $dAsset.properties.version) {
                    Write-Host "Discovered Asset $($dAsset.name) has a different version $($dAsset.properties.version)." `
                        -ForegroundColor Yellow
                    $needsSync = $true
                }
                else {
                    Write-Host "Asset $($asset.name) is up to date." -ForegroundColor Green
                    $onboardComplete = $true
                    continue
                }
            }

            # todo: Filter data points too
            #[array]$datasets = $dAsset.properties.datasets `
            #    | Select-Object -Property * -ExcludeProperty lastUpdatedOn
            # todo: Filter data points too
            #[array]$events = $dAsset.properties.events `
            #    | Select-Object -Property * -ExcludeProperty lastUpdatedOn
            #[array]$streams = $dAsset.properties.streams `
            #    | Select-Object -Property * -ExcludeProperty lastUpdatedOn
            #[array]$managementGroups = $dAsset.properties.managementGroups `
            #    | Select-Object -Property * -ExcludeProperty lastUpdatedOn

            $displayName = $dAsset.properties.displayName
            if (!$displayName) {
                $displayName = $dAsset.properties.model
            }
            Remove-PropertyRecursively -Object $dDevice.properties -PropertyName "lastUpdatedOn"
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
                    defaultDatasetsConfiguration = `
                        $dAsset.properties.defaultDatasetsConfiguration
                    defaultManagementGroupsConfiguration = `
                        $dAsset.properties.defaultManagementGroupsConfiguration
                    # .... add more properties as needed
                    deviceRef = $dAsset.properties.deviceRef
                    discoveredAssetRefs = @($dAsset.name)
                    assetTypeRefs = [array]$dAsset.properties.assetTypeRefs
                    datasets = $datasets
                    events = $events
                    streams = $streams
                    managementGroups = $managementGroups
                }
            } | ConvertTo-Json -Depth 100
            #$body | Out-Host
            $body | Out-File -FilePath $tempFile -Encoding utf8 -Force
            Write-Host "Create or update asset $($dAsset.name)..." -ForegroundColor Cyan
            $errOut = $($asset = & { az rest --method put `
                --url "$($ns.id)/assets/$($dAsset.name)?api-version=2025-07-01-preview" `
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
