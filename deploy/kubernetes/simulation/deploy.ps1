<#
    .SYNOPSIS
        Deploys OPC UA simulation servers in a Kubernetes cluster
        and registers them with Azure IoT Operations.

    .DESCRIPTION
        This script deploys a specified number of OPC UA simulation
        servers using Helm and registers them as devices in Azure IoT
        Operations. Each server is configured with specific endpoint
        settings and asset discovery capabilities.

    .PARAMETER SimulationName
        The type of simulation to deploy. Currently supports "opc-plc"
        and "opc-test". Default is "opc-plc".
    .PARAMETER Count
        Number of simulation servers to deploy.
    .PARAMETER InstanceName
        The name of the Azure IoT Operations instance
    .PARAMETER AdrNamespaceName
        The Azure IoT Operations namespace to deploy the simulation
        devices into. Must exist already.
    .PARAMETER Namespace
        The Kubernetes namespace where the simulation servers will
        be deployed.
    .PARAMETER ResourceGroup
        The resource group to create or use for the cluster.
        Default is the same as the cluster name.
    .PARAMETER SubscriptionId
        The subscription id to scope all activity to.
    .PARAMETER Location
        The Azure region where the resources will be created.
    .PARAMETER Force
        If specified, will force the creation of devices even if they
        already exist in the ADR namespace.
    .PARAMETER SkipLogin
        If specified, will skip the Azure login step.
#>

param(
    [string] [ValidateSet("opc-plc", "opc-test")] $SimulationName = "opc-plc",
    [int] $Count = 2,
    [string] [Parameter(Mandatory = $true)] $InstanceName,
    [string] $AdrNamespaceName,
    [string] $SubscriptionId,
    [string] $TenantId,
    [string] [Parameter(Mandatory = $true)] $ResourceGroup,
    [string] $Location = "westus",
    [string] $Namespace = "azure-iot-operations",
    [switch] $Force,
    [switch] $SkipLogin
)

$ErrorActionPreference = "Continue"
$scriptDirectory = Split-Path -Path $MyInvocation.MyCommand.Path

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

$deploymentName = "simulation"
$tempFile = New-TemporaryFile
Write-Host "Creating $($script:Count) $($script:SimulationName) simulations..." `
    -ForegroundColor Cyan
$helmPath = Join-Path $(Join-Path $scriptDirectory "helm") $script:SimulationName
helm upgrade -i $deploymentName "$($helmPath)" `
    --namespace $script:Namespace `
    --set simulations=$script:Count `
    --set deployDefaultIssuerCA=false `
    --wait

#
# Check adr namespace and azure iot instances exists
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
$errOut = $($iotOps = & { az iot ops show `
    --resource-group $rg.Name `
    --name $script:InstanceName `
    --subscription $SubscriptionId `
    --only-show-errors --output json } | ConvertFrom-Json) 2>&1
if (!$iotOps) {
    Write-Host "Error: Failed to get IoT Operations instance - $($errOut)." `
        -ForegroundColor Red
    exit -1
}

#
# Deployment simulation configuration
#
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

for ($i = 0; $i -lt $script:Count; $i++) {
    $suffix = $("{0:D6}" -f $i)
    $deviceName = "$($script:SimulationName)-$($suffix)"
    $deviceResource = "$($ns.id)/devices/$($deviceName)"
    $errOut = $($device = & { az rest --method get `
        --url "$($deviceResource)?api-version=2025-07-01-preview" `
        --headers "Content-Type=application/json" } | ConvertFrom-Json) 2>&1
    if (!$device -or !$device.id -or $script:Force.IsPresent) {
        $address = "$($script:SimulationName)-$($deploymentName)-$($suffix)"
        $address = "$($address).$($script:Namespace).svc.cluster.local"
        $address = "opc.tcp://$($address):50000"
        $body = @{
            extendedLocation = $iotOps.extendedLocation
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