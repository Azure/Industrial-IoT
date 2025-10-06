
<#
    .SYNOPSIS
        Setup local AIO cluster and connect to Arc (must run as admin)
    .DESCRIPTION
        Setup local AIO cluster and connect to Arc. This script installs
        the cluster type chosen and all other required dependencies and
        connect it to the cloud via Arc. Then it will install AIO on it.
    .NOTES
        DO NOT USE FOR PRODUCTION SYSTEMS. This script is intended for
        development and testing purposes only.

    .PARAMETER Name
        The name of the cluster
    .PARAMETER OpsInstanceName
        The name of the Azure IoT Ops instance. Default is the same as the
        cluster name.
    .PARAMETER AdrNamespaceName
        The name of the ADR namespace to create or use. Default is the same
        as the cluster name.
    .PARAMETER SchemaRegistryName
        The name of the schema registry to create or use. Default is the
        cluster name in lower case with "sr" suffix.
    .PARAMETER ResourceGroup
        The resource group to create or use for the cluster.
        Default is the same as the cluster name.
    .PARAMETER TenantId
        The tenant id to use when logging into Azure.
    .PARAMETER SubscriptionId
        The subscription id to scope all activity to.
    .PARAMETER Location
        The location of the cluster.
    .PARAMETER ClusterType
        The type of cluster to create. Default is microk8s.
    .PARAMETER ConnectorType
        Whether to deploy the OPC Publisher as connector. Official
        installs the official connector build, Local builds and deploys
        a local version. Debug the debug version. Default is Official.
    .PARAMETER BucketSize
        The size of the bucket to use to partition devices across
        connectors. Default is 1 where every connector gets one device.
    .PARAMETER NetworkDiscoveryMode
        Network discovery mode to use. Default is "Off" (disabled).
    .PARAMETER SkipLogin
        Skip the login to Azure. This is useful when running in a CI/CD
    .PARAMETER Force
        Force reinstall.
#>

param(
    [string] [Parameter(Mandatory = $true)] $Name,
    [string] $OpsInstanceName,
    [string] $AdrNamespaceName,
    [string] $SchemaRegistryName,
    [string] $ResourceGroup,
    [string] $TenantId,
    [string] $SubscriptionId,
    [string] $Location,
    [string] [ValidateSet(
        "kind",
        "minikube",
        "k3d",
        "microk8s"
    )] $ClusterType = "microk8s",
    [string] [ValidateSet(
        "Official",
        "Local",
        "Debug"
    )] $ConnectorType = "Official",
    [string] [ValidateSet(
        "Off",
        "Fast",
        "Local",
        "Full"
    )] $NetworkDiscoveryMode = "Off",
    [int] $BucketSize = 1,
    [switch] $SkipLogin,
    [switch] $Force
)
$ErrorActionPreference = 'Continue'

$scriptDirectory = Split-Path -Path $MyInvocation.MyCommand.Path
Import-Module $(Join-Path $(Join-Path $scriptDirectory "common") "cluster-utils.psm1") -Force

#
# Dump command line arguments
#
if ([string]::IsNullOrWhiteSpace($script:ResourceGroup)) {
    $script:ResourceGroup = $script:Name
    Write-Host "Using resource group $($script:ResourceGroup)..." -ForegroundColor Cyan
}
if ([string]::IsNullOrWhiteSpace($script:Location)) {
    $script:Location = "westus"
    Write-Host "Using location $($script:Location)..." -ForegroundColor Cyan
}
if ([string]::IsNullOrWhiteSpace($script:OpsInstanceName)) {
    $script:OpsInstanceName = $Name
    Write-Host "Using instance name $($script:OpsInstanceName)..." -ForegroundColor Cyan
}
if ([string]::IsNullOrWhiteSpace($script:AdrNamespaceName)) {
    $script:AdrNamespaceName = $Name
    Write-Host "Using ADR namespace name $($script:AdrNamespaceName)..." -ForegroundColor Cyan
}
if ([string]::IsNullOrWhiteSpace($script:SchemaRegistryName)) {
    $script:SchemaRegistryName = "$($Name.ToLowerInvariant())sr"
    Write-Host "Using schema registry name $($script:SchemaRegistryName)..." -ForegroundColor Cyan
}

# check if docker and az cli are installed
$errOut = $($docker = & { docker version --format json | ConvertFrom-Json }) 2>&1
if (-not $? -or !$docker.Server) {
    $docker | Out-Host
    Write-Host "Docker not installed or running : $errOut" -ForegroundColor Red
    exit -1
}
$errOut = $($azVersion = (az version)[1].Split(":")[1].Split('"')[1]) 2>&1
if ($azVersion -lt "2.74.0" -or !$azVersion) {
    Write-Host "Azure CLI version 2.74.0 or higher is required." `
        -ForegroundColor Red
    exit -1
}
$errOut = $($azVersion = & {az extension list -o json `
    --query "[?name == 'azure-iot-ops']"} | ConvertFrom-Json) 2>&1
if (!$azVersion -or $azVersion[0].version -lt "2.0.0") {
    Write-Host "Azure IoT Operations Extension version 2.0 or higher is required." `
        -ForegroundColor Red
    exit -1
}

#
# Log into azure
#
if (-not $script:SkipLogin.IsPresent -or -not $SubscriptionId) {
    if ([string]::IsNullOrWhiteSpace($script:TenantId)) {
        $script:TenantId = $env:AZURE_TENANT_ID
    }
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
# Check adr namespace schema registry and azure iot instance exists
#
$errOut = $($rg = & { az group show `
    --name $script:ResourceGroup `
    --subscription $script:SubscriptionId `
    --only-show-errors --output json } | ConvertFrom-Json) 2>&1
if (!$rg) {
    Write-Host "Resource group $($script:ResourceGroup) not found - $($errOut)." `
        -ForegroundColor Red
    exit -1
}
$adrNsResource = "/subscriptions/$($script:SubscriptionId)"
$adrNsResource = "$($adrNsResource)/resourceGroups/$($script:ResourceGroup)"
$adrNsResource = "$($adrNsResource)/providers/Microsoft.DeviceRegistry"
$adrNsResource = "$($adrNsResource)/namespaces/$($script:AdrNamespaceName)"
$errOut = $($ns = & { az rest --method get `
    --url "$($adrNsResource)?api-version=2025-10-01" `
    --headers "Content-Type=application/json" } | ConvertFrom-Json) 2>&1
if (!$ns -or !$ns.id) {
    Write-Host "ADR namespace $($script:AdrNamespaceName) not found - $($errOut)." `
        -ForegroundColor Red
    exit -1
}
$errOut = $($iotOps = & { az iot ops show `
    --resource-group $script:ResourceGroup `
    --name $script:OpsInstanceName `
    --subscription $script:SubscriptionId `
    --only-show-errors --output json } | ConvertFrom-Json) 2>&1
if (!$iotOps) {
    Write-Host "Azure IoT Operations instance $($script:OpsInstanceName) not found - $($errOut)." `
        -ForegroundColor Red
    exit -1
}
$errOut = $($sr = & { az iot ops schema registry show `
    --name $script:SchemaRegistryName `
    --resource-group $($rg.Name) `
    --subscription $script:SubscriptionId `
    --only-show-errors --output json } | ConvertFrom-Json) 2>&1
if (!$sr) {
    Write-Host "Schema registry $($script:SchemaRegistryName) not found. $($errOut)." `
        -ForegroundColor Red
    exit -1
}

#
# Deploy publisher as connector
#

$containerTag = "latest"
$containerRegistry = $null
$containerName = "iotedge/opc-publisher"
if ($script:ConnectorType -eq "Official") {
    Write-Host "Using official OPC Publisher image as connector..." -ForegroundColor Cyan
    $containerPull = "Always"
    $containerRegistry = @{
        registrySettingsType = "ContainerRegistry"
        containerRegistrySettings = @{
            registry = "mcr.microsoft.com"
        }
    }
    $containerTag = "2.9.15-preview6" # TODO: Remove
    $containerImage = "mcr.microsoft.com/$($containerName):$($containerTag)"
    $connectorMetadataRef = "mcr.microsoft.com/$($containerName):$($containerTag)-metadata"
}
else {
    $projFile = "Azure.IIoT.OpcUa.Publisher.Module"
    $projFile = "../../src/$($projFile)/src/$($projFile).csproj"
    $configuration = $script:ConnectorType
    if ($configuration -eq "Local") {
        $configuration = "Release"
    }
    $containerTag = Get-Date -Format "MMddHHmmss"
    $containerImage = "$($containerName):$($containerTag)"
    $connectorMetadataRef = $null
    Write-Host "Publishing $configuration OPC Publisher as $containerImage..." `
        -ForegroundColor Cyan
    dotnet restore $projFile -s https://api.nuget.org/v3/index.json
    dotnet publish $projFile -c $configuration --self-contained false --no-restore `
        /t:PublishContainer -r linux-x64 /p:ContainerImageTag=$($containerTag)
    if (-not $?) {
        Write-Host "Error building opc publisher connector." -ForegroundColor Red
        exit -1
    }
    Write-Host "$configuration container image $containerImage published successfully." `
        -ForegroundColor Green
    $containerPull = "IfNotPresent"

    # Import container image
    Import-ContainerImage -ClusterType $script:ClusterType -ContainerImage $containerImage
    if ($script:ClusterType -eq "microk8s") {
        $containerImage = "docker.io/$($containerImage)"
        $containerRegistry = @{
            registrySettingsType = "ContainerRegistry"
            containerRegistrySettings = @{
                registry = "docker.io"
            }
        }
    }
}

# Deploy connector template
$tempFile = New-TemporaryFile
$template = @{
    extendedLocation = $iotOps.extendedLocation
    properties = @{
        aioMetadata = @{
            aioMinVersion = "1.2.*"
            aioMaxVersion = "1.*.*"
        }
        runtimeConfiguration = @{
            runtimeConfigurationType = "ManagedConfiguration"
            managedConfigurationSettings = @{
                managedConfigurationType = "ImageConfiguration"
                imageConfigurationSettings = @{
                    imageName = $containerName
                    imagePullPolicy = $containerPull
                    replicas = 1 # set to 2 when HA is supported
                    registrySettings = $containerRegistry
                    tagDigestSettings = @{
                        tagDigestType = "Tag"
                        tag = $containerTag
                    }
                }
                allocation = @{
                    policy = "Bucketized"
                    bucketSize = $script:BucketSize
                }
                additionalConfiguration = @{
                    EnableMetrics = "True"
                    UseFileChangePolling = "True"
                    AioNetworkDiscoveryMode = $null
                    AioNetworkDiscoveryInterval = $null
                    DisableDataSetMetaData = "True"
                    LogFormat = "syslog"
                    # Needed because we are not running as "app" user
                    PkiRootPath = "/var/tmp/pki"
                    PublishedNodesFile = "/var/tmp/pn.json"
                    CreatePublishFileIfNotExist = "True"
                }
                #persistentVolumeClaims = @(
                #    @{
                #        claimName = "opcpublisherdata"
                #        mountPath = "/app"
                #    }
                #)
                secrets = @()
            }
        }
        deviceInboundEndpointTypes = @(
            @{
                endpointType = "Microsoft.OpcPublisher"
                version = "2.9"
                displayName = "OPC Publisher"
            }
        )
        diagnostics = @{
            logs = @{
                level = "info"
            }
        }
        connectorMetadataRef = $connectorMetadataRef
        mqttConnectionConfiguration = @{
            host = "aio-broker:18883"
            authentication = @{
                method = "ServiceAccountToken"
                serviceAccountTokenSettings = @{
                    audience = "aio-internal"
                }
            }
            tls = @{
                mode = "Enabled"
                trustedCaCertificateConfigMapRef = "azure-iot-operations-aio-ca-trust-bundle"
            }
        }
    }
} | ConvertTo-Json -Depth 100

# Workaround for discovery while discovery handler is not yet supported
if ($script:NetworkDiscoveryMode -ne "Off") {
    Write-Host "Network Discovery via discovery handler is not supported yet." `
         -ForegroundColor Yellow
    Write-Host "Enabling discovery in connector instead." `
         -ForegroundColor Yellow
    $additionalConfig = $template.properties.runtimeConfiguration.additionalConfiguration
    $additionalConfig.AioNetworkDiscoveryMode = $script:NetworkDiscoveryMode
    $additionalConfig.AioNetworkDiscoveryInterval = "00:10:00"
    $script:NetworkDiscoveryMode = "Off"
}

$template | Out-File -FilePath $tempFile -Encoding utf8 -Force

$ctName = "opc-publisher"
$ctResource = "/subscriptions/$($script:SubscriptionId)"
$ctResource = "$($ctResource)/resourceGroups/$($rg.Name)"
$ctResource = "$($ctResource)/providers/Microsoft.IoTOperations"
$ctResource = "$($ctResource)/instances/$($iotOps.name)"
$ctResource = "$($ctResource)/akriConnectorTemplates/$($ctName)"
Write-Host "Deploying connector template $($ctName)..." -ForegroundColor Cyan
az rest --method put `
    --url "$($ctResource)?api-version=2025-09-01-preview" `
    --headers "Content-Type=application/json" `
    --body @$tempFile
if (-not $?) {
    Write-Host "Error deploying connector template $($ctName) - $($errOut)." `
        -ForegroundColor Red
    Remove-Item -Path $tempFile -Force
    exit -1
}

# Deploy discovery handler - disabled in current version of Azure IoT Operations
if ($script:NetworkDiscoveryMode -ne "Off") {
    $template = @{
        extendedLocation = $iotOps.extendedLocation
        properties = @{
            aioMetadata = @{
                aioMinVersion = "1.2.*"
                aioMaxVersion = "1.*.*"
            }
            imageConfiguration = @{
                imageName = $containerName
                imagePullPolicy = $containerPull
                registrySettings = $containerRegistry
                tagDigestSettings = @{
                    tagDigestType = "Tag"
                    tag = $containerTag
                }
            }
            mode = "Enabled"
            schedule = @{
                scheduleType = "Cron" # or "Continuous" or "RunOnce"
                cron = "*/10 * * * *"
            }
            additionalConfiguration = @{
                AioNetworkDiscoveryMode = "$($script:NetworkDiscoveryMode)"
                EnableMetrics = "True"
                UseFileChangePolling = "True"
                LogFormat = "syslog"
                DisableDataSetMetaData = "True"
                # Needed because we are not running as "app" user
                PkiRootPath = "/var/tmp/pki"
                PublishedNodesFile = "/var/tmp/pn.json"
                CreatePublishFileIfNotExist = "True"
            }
            discoverableDeviceEndpointTypes = @(
                @{
                    endpointType = "Microsoft.OpcPublisher"
                    version = "2.9"
                    #displayName = "OPC Publisher"
                }
            )
            secrets = @()
            diagnostics = @{
                logs = @{
                    level = "info"
                }
            }
            connectorMetadataRef = $connectorMetadataRef
            mqttConnectionConfiguration = @{
                host = "aio-broker:18883"
                authentication = @{
                    method = "ServiceAccountToken"
                    serviceAccountTokenSettings = @{
                        audience = "aio-internal"
                    }
                }
                tls = @{
                    mode = "Enabled"
                    trustedCaCertificateConfigMapRef = "azure-iot-operations-aio-ca-trust-bundle"
                }
            }
        }
    } | ConvertTo-Json -Depth 100
    $template | Out-File -FilePath $tempFile -Encoding utf8 -Force

    $dhName = "opc-publisher"
    $dhResource = "/subscriptions/$($script:SubscriptionId)"
    $dhResource = "$($dhResource)/resourceGroups/$($rg.Name)"
    $dhResource = "$($dhResource)/providers/Microsoft.IoTOperations"
    $dhResource = "$($dhResource)/instances/$($iotOps.name)"
    $dhResource = "$($dhResource)/akriDiscoveryHandlers/$($dhName)"
    Write-Host "Deploying discovery handler template $($dhName)..." -ForegroundColor Cyan
    az rest --method put `
        --url "$($dhResource)?api-version=2025-09-01-preview" `
        --headers "Content-Type=application/json" `
        --body @$tempFile
    if (-not $?) {
        Write-Host "Error deploying discovery handler template $($dhName) - $($errOut)." `
            -ForegroundColor Red
        Remove-Item -Path $tempFile -Force
        exit -1
    }
}

Remove-Item -Path $tempFile -Force

