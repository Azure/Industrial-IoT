<#
    .SYNOPSIS
        Deploys OPC UA simulation servers in a Kubernetes cluster
        and registers them with Azure IoT Operations.
    .DESCRIPTION
        This script deploys a specified number of OPC UA simulation
        servers using Helm and registers them as devices in Azure IoT
        Operations. Each server is configured with specific endpoint
        settings and asset discovery capabilities.
    .NOTES
        DO NOT USE FOR PRODUCTION SYSTEMS. This script is intended for
        development and testing purposes only.

    .PARAMETER SimulationName
        The type of simulation to deploy. Currently supports "opc-plc"
        and "opc-test". Default is "opc-plc".
    .PARAMETER Count
        Number of simulation servers to deploy.
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
    .PARAMETER InstanceName
        The name of the Azure IoT Operations instance to register
        the devices with. This instance must already exist.
    .PARAMETER DeploymentName
        The name of the Helm deployment for the simulation servers.
    .PARAMETER Force
        If specified, will force the creation of devices even if they
        already exist in the ADR namespace.
    .PARAMETER SkipLogin
        If specified, will skip the Azure login step.
    .PARAMETER ClusterType
        The type of Kubernetes cluster to deploy to. Valid values are
        "kind", "minikube", "k3d", and "microk8s". Optional and only
        needed if you want to deploy opc-test from local repo.
#>

param(
    [string] $DeploymentName = "simulation",
    [string] [ValidateSet(
        "opc-plc",
        "opc-test",
        "umati"
    )] $SimulationName = "opc-plc",
    [int] $Count = 1,
    [string] [Parameter(Mandatory = $true)] $InstanceName,
    [string] [Parameter(Mandatory = $true)] $ResourceGroup,
    [string] $AdrNamespaceName,
    [string] $SubscriptionId,
    [string] $TenantId,
    [string] $Location = "westus",
    [string] $Namespace = "azure-iot-operations",
    [string] [ValidateSet(
        "kind",
        "minikube",
        "k3d",
        "microk8s"
    )] $ClusterType,
    [switch] $Force,
    [switch] $SkipLogin
)

$ErrorActionPreference = "Continue"
$scriptDirectory = Split-Path -Path $MyInvocation.MyCommand.Path
Import-Module $(Join-Path $(Join-Path $(Split-Path $scriptDirectory) "common") `
    "cluster-utils.psm1") -Force

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

$displayName = "$($script:Count) $($script:SimulationName) simulation servers"
if ($script:SimulationName -eq "umati") {
    Write-Host "Using public Umati server for simulation." -ForegroundColor Yellow
    $script:Count = 1
}
elseif ($script:SimulationName -eq "opc-test" -and $script:ClusterType) {
    Write-Host "Building opc-test-server image for simulation." -ForegroundColor Yellow
    $projFile = "Azure.IIoT.OpcUa.Publisher.Testing"
    $projFile = "../../../src/$($projFile)/cli/$($projFile).Cli.csproj"
    $configuration = "Debug"
    $containerTag = "latest"
    $containerName = "iot/opc-ua-test-server"
    $containerImage = "$($containerName):$($containerTag)"
    Write-Host "Publishing $configuration Simulation as $containerImage..." `
        -ForegroundColor Cyan
    dotnet restore $projFile -s https://api.nuget.org/v3/index.json
    dotnet publish $projFile -c $configuration --self-contained false --no-restore `
        /t:PublishContainer -r linux-x64 /p:ContainerImageTag=$($containerTag)
    if (-not $?) {
        Write-Host "Error building opc-ua-test server image connector." -ForegroundColor Red
        exit -1
    }
    Write-Host "$configuration container image $containerImage published successfully." `
        -ForegroundColor Green
    Import-ContainerImage -ClusterType $script:ClusterType -ContainerImage $containerImage
    $helmPath = Join-Path $(Join-Path $scriptDirectory "helm") $script:SimulationName
    helm upgrade -i $script:DeploymentName "$($helmPath)" `
        --namespace $script:Namespace `
        --set simulations=$script:Count `
        --set deployDefaultIssuerCA=false `
        --set image=$($containerName) `
        --set tag=$($containerTag) `
        --set pullPolicy=IfNotPresent `
        --wait
    if (-not $?) {
        Write-Host "Error deploying $($displayName) as $($script:DeploymentName)." `
            -ForegroundColor Red
        exit -1
    }
    Write-Host "Deployment of $($displayName) as $($script:DeploymentName) completed." `
        -ForegroundColor Green
}
else {
    Write-Host "Deploying $($displayName) as $($script:DeploymentName)..." `
        -ForegroundColor Cyan
    $helmPath = Join-Path $(Join-Path $scriptDirectory "helm") $script:SimulationName
    helm upgrade -i $script:DeploymentName "$($helmPath)" `
        --namespace $script:Namespace `
        --set simulations=$script:Count `
        --set deployDefaultIssuerCA=false `
        --wait

    if (-not $?) {
        Write-Host "Error deploying $($displayName) as $($script:DeploymentName)." `
            -ForegroundColor Red
        exit -1
    }
    Write-Host "Deployment of $($displayName) as $($script:DeploymentName) completed." `
        -ForegroundColor Green
}

$tempFile = New-TemporaryFile

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
    --resource-group $script:ResourceGroup `
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
switch ($script:SimulationName) {
    "opc-plc" {
        # OPC Plc
        $assetTypes += "nsu=http://opcfoundation.org/UA/Boiler/;i=1132"
        $assetTypes += "nsu=http://microsoft.com/Opc/OpcPlc/Boiler;i=1000"
        $assetTypes += "nsu=http://microsoft.com/Opc/OpcPlc/Boiler;i=3"
    }
    "opc-test" {
        $assetTypes += "nsu=http://opcfoundation.org/UA/Boiler/;i=1132" # BoilerType
        $assetTypes += "nsu=http://opcfoundation.org/UA/WoT-Con/;i=115" # AssetType
        #$assetTypes += "nsu=FileSystem;i=16314" # FileSystemType
    }
    "umati" {
        # $assetTypes += "nsu=http://opcfoundation.org/UA/MachineTool/;i=14" # monitoring
        $assetTypes += "nsu=http://opcfoundation.org/UA/MachineTool/;i=13" # machine tool
    }
}

for ($i = 0; $i -lt $script:Count; $i++) {
    $suffix = $("{0:D6}" -f $i)
    $deviceName = "$($script:SimulationName)-$($suffix)"
    $deviceResource = "$($ns.id)/devices/$($deviceName)"
    $errOut = $($device = & { az rest --method get `
        --url "$($deviceResource)?api-version=2025-07-01-preview" `
        --headers "Content-Type=application/json" } | ConvertFrom-Json) 2>&1
    if (!$device -or !$device.id -or $script:Force.IsPresent) {
        if ($script:SimulationName -eq "umati") { 
            $address = "opc.tcp://opcua.umati.app:4840"
        }
        else {
            $address = "$($script:SimulationName)-$($DeploymentName)-$($suffix)"
            $address = "$($address).$($script:Namespace).svc.cluster.local"
            $address = "opc.tcp://$($address):50000"
        }
        $body = @{
            extendedLocation = $iotOps.extendedLocation
            location = $Location
            properties = @{
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
