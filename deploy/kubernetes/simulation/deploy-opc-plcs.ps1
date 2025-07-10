<#
.SYNOPSIS
    Deploys OPC PLC simulation servers in a Kubernetes cluster
    and registers them with Azure IoT Operations.

.DESCRIPTION
    This script deploys a specified number of OPC PLC simulation
    servers using Helm and registers them as devices in Azure IoT
    Operations. Each server is configured with specific endpoint
    settings and asset discovery capabilities.

.PARAMETER InstanceNamespace
    The Kubernetes namespace where the simulation servers will
    be deployed.
.PARAMETER Location
    The Azure region where the resources will be created.
.PARAMETER AdrNsResourceId
    The Azure IoT Operations namespace resource id.
.PARAMETER ExtendedLocation
    The Azure IoT Operations instance's ExtendedLocation property.
.PARAMETER NumberOfDevices
    Number of OPC PLC simulation servers to deploy.
#>

param(
    [string] $InstanceNamespace = "azure-iot-operations",
    [string] $Location = "westus",
    [string] $AdrNsResourceId,
    [object] $ExtendedLocation,
    [string] [ValidateSet(
        "opc-plc"
    )] $SimulationName = "opc-plc",
    [int] $NumberOfDevices = 2,
    [switch] $Force
)

$ErrorActionPreference = "Continue"
$scriptDirectory = Split-Path -Path $MyInvocation.MyCommand.Path

$assetTypes = @("i=2004")
if ($script:SimulationName -eq "opc-plc") {
    # OPC Plc
    $assetTypes += "nsu=http://opcfoundation.org/UA/Boiler/;i=1132"
    $assetTypes += "nsu=http://microsoft.com/Opc/OpcPlc/Boiler;i=1000"
    $assetTypes += "nsu=http://microsoft.com/Opc/OpcPlc/Boiler;i=3"
}
elseif ($script:SimulationName -eq "opc-test") {
    # OPC Test
    # todo
}

$tempFile = New-TemporaryFile
Write-Host "Creating $($script:NumberOfDevices) $($script:SimulationName) simulations..." `
    -ForegroundColor Cyan
$helmPath = Join-Path $(Join-Path $scriptDirectory "helm") $script:SimulationName
helm upgrade -i simulation "$($helmPath)" `
    --namespace $script:InstanceNamespace `
    --set simulations=$script:NumberOfDevices `
    --set deployDefaultIssuerCA=false `
    --wait

if (-not $script:ExtendedLocation -or -not $script:AdrNsResourceId) {
    Write-Host "ExtendedLocation or AdrNsResourceId not provided. Skipping device creation." `
        -ForegroundColor Yellow
    return
}

for ($i = 0; $i -lt $script:NumberOfDevices; $i++) {
    $deviceName = "$($script:SimulationName)-$("{0:D6}" -f $i)"
    $deviceResource = "$($script:AdrNsResourceId)/devices/$($deviceName)"
    $errOut = $($device = & { az rest --method get `
        --url "$($deviceResource)?api-version=2025-07-01-preview" `
        --headers "Content-Type=application/json" } | ConvertFrom-Json) 2>&1
    if (!$device -or !$device.id -or $script:Force.IsPresent) {
        $address = "$($script:SimulationName)-$($deviceName).$($script:InstanceNamespace)"
        $address = "opc.tcp://$($address).svc.cluster.local:50000"
        $body = @{
            extendedLocation = $script:ExtendedLocation
            location = $Location
            properties = @{
                # externalDeviceId = "unique-edge-device-identifier"
                enabled = $true
                attributes = @{
                    deviceType = "LDS"
                }
                endpoints = @{
                    inbound = @{
                        "none" = @{
                            address = $address
                            endpointType = "Microsoft.OpcPublisher"
                            version = "2.9"
                            authentication = @{
                                method = "Anonymous"
                            }
                            additionalConfiguration = $(@{
                                EndpointSecurityMode = "None"
                                EndpointSecurityPolicy = "None"
                                RunAssetDiscovery = $True
                                AssetTypes = $assetTypes
                            } | ConvertTo-Json -Depth 100 -Compress)
                        }
                    }
                }
            }
        } | ConvertTo-Json -Depth 100
        $body | Out-File -FilePath $tempFile -Encoding utf8 -Force
        Write-Host "Creating ADR namespaced device $deviceResource..." -ForegroundColor Cyan
        $errOut = $($device = & { az rest --method put `
            --url "$($deviceResource)?api-version=2025-07-01-preview" `
            --headers "Content-Type=application/json" `
            --body @$tempFile } | ConvertFrom-Json) 2>&1
        if (-not $? -or !$device -or !$device.id) {
            Write-Host "Error: Failed to create device $($deviceResource) - $($errOut)." `
                -ForegroundColor Red
            Remove-Item -Path $tempFile -Force
            exit -1
        }
        Write-Host "ADR namespaced device $($device.id) created." -ForegroundColor Green
    }
    else {
        Write-Host "ADR namespaced device $($device.id) exists." -ForegroundColor Green
    }
}
Remove-Item -Path $tempFile -Force