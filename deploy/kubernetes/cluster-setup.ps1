
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
        The name of the instance to create. Default is the same as the
        cluster name.
    .PARAMETER ClusterNamespace
        The namespace to create the instance in.
        Default is azure-iot-operations.
    .PARAMETER ResourceGroup
        The resource group to create or use for the cluster.
        Default is the same as the cluster name.
    .PARAMETER SharedFolderPath
        The shared folder path on the host system to mount into the guest.
    .PARAMETER TenantId
        The tenant id to use when logging into Azure.
    .PARAMETER SubscriptionId
        The subscription id to scope all activity to.
    .PARAMETER Location
        The location of the cluster.
    .PARAMETER ClusterType
        The type of cluster to create. Default is microk8s.
    .PARAMETER Connector
        Whether to deploy the OPC Publisher as connector. Official
        installs the official connector build, Local builds and deploys
        a local version. Debug the debug version. Default is Official.
    .PARAMETER OpsVersion
        The version of Azure IoT Operations to use.
        Default is the latest stable release.
    .PARAMETER OpsTrain
        The train to use if an OpsVersion is chosen.
        Default is integration.
    .PARAMETER Force
        Force reinstall.
    .PARAMETER OpsExtension
        (Internal) Use a preview version of the Azure IoT Operations
        extension to use.
    .PARAMETER DeployPlcSimulation
        Whether to deploy the PLC simulation. Default is false.
    .PARAMETER DeployTestSimulation
        Whether to deploy the Test simulation. Default is false.
#>

param(
    [string] [Parameter(Mandatory = $true)] $Name,
    [string] $OpsInstanceName,
    [string] $SharedFolderPath,
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
    [string] $ClusterNamespace,
    [string] [ValidateSet(
        "None",
        "Official",
        "Local",
        "Debug"
    )] $Connector = "Official",
    [string] $OpsVersion = $null,
    [string] [ValidateSet(
        "integration",
        "stable",
        "dev"
    )] $OpsTrain = "integration",
    [switch] $Force,
    [string] [ValidateSet(
        "preview",
        "installed",
        "stable"
    )] $OpsExtension = "stable",
    [switch] $DeployPlcSimulation,
    [switch] $DeployTestSimulation,
    [switch] $EnableNetworkDiscovery
)

#Requires -RunAsAdministrator
$ErrorActionPreference = 'Continue'

if (![Environment]::Is64BitProcess) {
    Write-Host "Error: Run this in 64bit Powershell session" -ForegroundColor Red
    exit -1
}

$scriptDirectory = Split-Path -Path $MyInvocation.MyCommand.Path
Import-Module $(Join-Path $(Join-Path $scriptDirectory "common") "cluster-utils.psm1") -Force

#
# Dump command line arguments
#
$forceReinstall = $script:Force.IsPresent
if ($forceReinstall) {
    Write-Host "Force reinstall..." -ForegroundColor Yellow
}
if ([string]::IsNullOrWhiteSpace($script:ResourceGroup)) {
    $script:ResourceGroup = $script:Name
}
Write-Host "Using resource group $($script:ResourceGroup)..." -ForegroundColor Cyan
if ([string]::IsNullOrWhiteSpace($script:TenantId)) {
    $script:TenantId = $env:AZURE_TENANT_ID
}
if (![string]::IsNullOrWhiteSpace($script:TenantId)) {
    Write-Host "Using tenant $($script:TenantId)..." -ForegroundColor Cyan
}
if ([string]::IsNullOrWhiteSpace($script:Location)) {
    $script:Location = "westus"
}
Write-Host "Using location $($script:Location)..." -ForegroundColor Cyan
if ([string]::IsNullOrWhiteSpace($script:OpsInstanceName)) {
    $script:OpsInstanceName = $script:Name
}
Write-Host "Using instance name $($script:OpsInstanceName)..." -ForegroundColor Cyan
if ([string]::IsNullOrWhiteSpace($script:ClusterNamespace)) {
    $script:ClusterNamespace = "azure-iot-operations"
}
else {
    Write-Host "   ... with cluster namespace $($script:ClusterNamespace)..." `
        -ForegroundColor Cyan
}
if (![string]::IsNullOrEmpty($script:OpsVersion)) {
    Write-Host "   ... and version $($script:OpsVersion) ($($script:OpsTrain))..." `
        -ForegroundColor Cyan
}
$mountPath = "C:\Shared"
if (![string]::IsNullOrWhiteSpace($script:SharedFolderPath)) {
    $mountPath = $script:SharedFolderPath
}
#Write-Host "Using shared folder path $mountPath..." -ForegroundColor Cyan
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

# Install az cli if not installed or version is too old
$installAz = $false
try {
    $azVersion = (az version)[1].Split(":")[1].Split('"')[1]
    if ($azVersion -lt "2.74.0" -or !$azVersion) {
        $installAz = $true
    }
}
catch {
    $installAz = $true
}
if ($installAz) {
    Write-Host "Installing Az CLI..." -ForegroundColor Cyan

    Set-ExecutionPolicy Bypass -Scope Process -Force
    $ProgressPreference = 'SilentlyContinue'
    Invoke-WebRequest -Uri https://aka.ms/installazurecliwindowsx64 -OutFile .\AzureCLI.msi
    Start-Process msiexec.exe -Wait -ArgumentList '/I AzureCLI.msi /quiet'
    Remove-Item .\AzureCLI.msi
}
else {
    Write-Host "Az CLI version $azVersion found." -ForegroundColor Green
}

#
# Log into azure
#
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
if ([string]::IsNullOrWhiteSpace($SubscriptionId)) {
    $script:SubscriptionId = $session[0].id
}
if ([string]::IsNullOrWhiteSpace($TenantId)) {
    $script:TenantId = $session[0].tenantId
}

Write-Host "Ensuring all required dependencies are installed..." -ForegroundColor Cyan

if ($script:ClusterType -ne "none" `
    -and $script:ClusterType -ne "microk8s" `
    -and $script:Connector -ne "None") {
    # check if docker is installed
    $errOut = $($docker = & { docker version --format json | ConvertFrom-Json }) 2>&1
    if (-not $? -or !$docker.Server) {
        $docker | Out-Host
        Write-Host "Docker not installed or running : $errOut" -ForegroundColor Red
        exit -1
    }
    Write-Host "Found $($docker.Server.Platform.Name)..." -ForegroundColor Green
}

# Install required az extensions
az config set extension.dynamic_install_allow_preview=true --only-show-errors 2>&1 `
    | Out-Null
$ensureLatest = "false"
$extensions = @( "connectedk8s", "k8s-configuration" )
if ($script:OpsExtension -eq "stable"){
    $ensureLatest = "true"
    $extensions += "azure-iot-ops"
}
elseif ($script:OpsExtension -eq "preview") {
    $query = "[?!contains(name, '255')].{ Name:name, Date:properties.creationTime }"
    $iotOpsWhl = $($(az storage blob list `
        --container-name drop --account-name azedgecli --auth-mode login `
        --query "max_by($query, &Date)" `
        --output json) | ConvertFrom-Json).Name
    if ((-not $?) -or (-not $iotOpsWhl)) {
        Write-Host "Error: No preview extension found." -ForegroundColor Red
        exit -1
    }
    else {
        $extensionVersion = $iotOpsWhl `
            -replace "azext_iot_ops-", "" -replace "-py3-none-any.whl", ""
        # Create a temp folder
        $temp = New-Item -ItemType Directory -Path ([System.IO.Path]::GetTempPath()) `
            -Name ([System.Guid]::NewGuid().ToString()) `
            | Select-Object -ExpandProperty FullName
        $ext = "$($temp)/$($iotOpsWhl)"
        $errOut = $($stdOut = & { az storage blob download `
            --auth-mode login `
            --container-name drop `
            --account-name azedgecli `
            --name $iotOpsWhl `
            --file $ext }) 2>&1
        if ($?) {
            $errOut = $($stdOut = & { az extension add --allow-preview true `
                --upgrade --yes --source $ext }) 2>&1
        }
        if (-not $?) {
            Write-Host "Error installing iot ops extension $($extensionVersion) - $($errOut)." `
                -ForegroundColor Red
            exit -1
        }
        Write-Host "Using iot ops extension version $($extensionVersion)..." `
            -ForegroundColor Cyan
    }
}
foreach ($p in $extensions) {
    $errOut = $($stdOut = & { az extension add `
        --upgrade `
        --name $p `
        --allow-preview true }) 2>&1
    if (-not $?) {
        $stdOut | Out-Host
        Write-Host "Error installing az extension $p : $errOut" -ForegroundColor Red
        exit -1
    }
}
if (![string]::IsNullOrWhiteSpace($SubscriptionId)) {
    az account set --subscription $SubscriptionId 2>&1 | Out-Null
}

# Install chocolatey
$errOut = $($chocolateyVersion = & { choco --version }) 2>&1
if (!$chocolateyVersion -or $errOut) {
    Write-Host "Installing Chocolatey CLI..." -ForegroundColor Cyan
    [System.Net.ServicePointManager]::SecurityProtocol = `
        [System.Net.ServicePointManager]::SecurityProtocol -bor 3072
    $scriptLoc = 'https://community.chocolatey.org/install.ps1'
    Invoke-Expression ((New-Object System.Net.WebClient).DownloadString($scriptLoc))
}

# Install or upgrade kind, helm and kubectl, k9s and headlamp
$packages =
@(
    "kubernetes-cli",
    "kubernetes-helm",
    "headlamp",
    "k9s"
)
if ($script:ClusterType -ne "none" -and $script:ClusterType -ne "microk8s") {
    $packages += $script:ClusterType
}
foreach ($p in $packages) {
    $errOut = $($stdOut = & { choco install $p --yes }) 2>&1
    if (-not $?) {
        $stdOut | Out-Host
        Write-Host "Error during chocolatey install of $p : $errOut" -ForegroundColor Red
        exit -1
    }
    $errOut = $($stdOut = & { choco upgrade $p --yes }) 2>&1
    if (-not $?) {
        $stdOut | Out-Host
        Write-Host "Error during chocolatey upgrade of $p : $errOut" -ForegroundColor Red
        exit -1
    }
}

#
# Create the cluster
#
if ($script:ClusterType -eq "none") {
    Write-Host "Skipping cluster creation..." -ForegroundColor Green
}
elseif ($script:ClusterType -eq "microk8s") {
    # ensure multipass is running
    Start-Service -Name "Multipass" -ErrorAction SilentlyContinue | Out-Null
    $errOut = $($stdOut = & { microk8s status }) 2>&1
    if (-not $?) {
        Write-Host "Error querying microk8s status - $($errOut)" -ForegroundColor Red
        exit -1
    }
    if (!$forceReinstall -and $stdOut -match "microk8s is running") {
        Write-Host "Microk8s cluster is running..." -ForegroundColor Green
    }
    else {
        Write-Host "Resetting microk8s cluster..." -ForegroundColor Cyan
        microk8s uninstall
        microk8s install --cpu 4 --mem 12
        microk8s start
        if (-not $?) {
            Write-Host "Error starting microk8s cluster - $($errOut)" -ForegroundColor Red
            exit -1
        }
        Write-Host "Microk8s cluster started." -ForegroundColor Green
    }
    $features =
    @(
        "dns",
        "hostpath-storage",
        "ingress"
    )
    foreach ($f in $features) {
        $errOut = $($stdOut = & { microk8s enable $f }) 2>&1
        if (-not $?) {
            $stdOut | Out-Host
            Write-Host "Error enabling microk8s feature $f : $errOut" `
                -ForegroundColor Red
            exit -1
        }
    }
    $(microk8s config) | Out-File $env:USERPROFILE/.kube/config -Encoding utf8 -Force
}
elseif ($script:ClusterType -eq "k3d") {
    $errOut = $($table = & { k3d cluster list --no-headers } -split "`n") 2>&1
    if (-not $?) {
        Write-Host "Error querying k3d clusters - $($errOut)" -ForegroundColor Red
        exit -1
    }
    $clusters = $table | ForEach-Object { $($_ -split " ")[0].Trim() }
    if (($clusters -contains $script:Name) -and (!$forceReinstall)) {
        Write-Host "Cluster $script:Name exists..." -ForegroundColor Green
    }
    else {
        foreach ($cluster in $clusters) {
            if (!$forceReinstall) {
                if ($(Read-Host "Delete existing cluster $cluster? [Y/N]") -ne "Y") {
                    continue
                }
            }
            Write-Host "Deleting existing cluster $cluster..." -ForegroundColor Yellow
            k3d cluster delete $cluster 2>&1 | Out-Null
        }
        Write-Host "Creating k3d cluster $script:Name..." -ForegroundColor Cyan

        $fullPath1 = Join-Path $mountPath "system"
        if (!(Test-Path $fullPath1)) {
            New-Item -ItemType Directory -Path $fullPath1 | Out-Null
        }
        $volumeMapping1 = "$($fullPath1):/var/lib/rancher/k3s/storage@all"
        $fullPath2 = Join-Path $mountPath "user"
        if (!(Test-Path $fullPath2)) {
            New-Item -ItemType Directory -Path $fullPath2 | Out-Null
        }
        $volumeMapping2 = "$($fullPath2):/storage/user@all"
        $env:K3D_FIX_MOUNTS = 1
        k3d cluster create $script:Name `
            --agents 3 `
            --servers 1 `
            --volume $volumeMapping1 `
            --volume $volumeMapping2 `
            --env K3D_FIX_MOUNTS=1@all `
            --wait
        if (-not $?) {
            Write-Host "Error creating k3d cluster - $($errOut)" -ForegroundColor Red
            exit -1
        }
        Write-Host "Cluster created..." -ForegroundColor Green
    }
}
elseif ($script:ClusterType -eq "minikube") {
    $errOut = $($clusters = & { minikube profile list -o json } | ConvertFrom-Json) 2>&1
    if (($clusters.valid.Name -contains $script:Name) -and (!$forceReinstall)) {
        Write-Host "Valid minikube cluster $script:Name exists..." -ForegroundColor Green
        # Start the cluster
        if ($stat.Host -Contains "Stopped" -or
            $stat.APIServer -Contains "Stopped" -or
            $stat.Kubelet -Contains "Stopped") {
            Write-Host "Minikube cluster $script:Name stopped. Starting..." -ForegroundColor Cyan
            minikube start -p $script:Name
            if (-not $?) {
                Write-Host "Error starting minikube cluster." -ForegroundColor Red
                minikube logs --file=$($script:Name).log
                exit -1
            }
            Write-Host "Minikube cluster $script:Name started." -ForegroundColor Green
        }
        else {
            Write-Host "Minikube cluster $script:Name running." -ForegroundColor Green
        }
    }
    elseif (-not $?) {
        Write-Host "Error querying minikube clusters - $($errOut)" -ForegroundColor Red
        exit -1
    }
    else {
        if ($forceReinstall) {
            Write-Host "Deleting other clusters..." -ForegroundColor Yellow
            minikube delete --all --purge
        }
        elseif ($clusters.invalid.Name -contains $script:Name) {
            Write-Host "Delete bad minikube cluster $script:Name..." -ForegroundColor Yellow
            minikube delete -p $script:Name
        }
        Write-Host "Creating new minikube cluster $script:Name..." -ForegroundColor Cyan

        if (Get-WindowsOptionalFeature -Online -FeatureName Microsoft-Hyper-V-All) {
            Write-Host "Hyper-V is enabled..." -ForegroundColor Green
        }
        else {
            Write-Host "Enabling Hyper-V..." -ForegroundColor Cyan
            $hv = Enable-WindowsOptionalFeature -Online `
                -FeatureName Microsoft-Hyper-V-All
            if ($hv.RestartNeeded) {
                Write-Host "Restarting..." -ForegroundColor Yellow
                Restart-Computer -Force
            }
        }

        if (Get-WindowsOptionalFeature -Online -FeatureName Microsoft-Hyper-V-Management-PowerShell) {
            Write-Host "Hyper-V commands installed..." -ForegroundColor Green
        }
        else {
            $hv = Enable-WindowsOptionalFeature -Online `
                -FeatureName Microsoft-Hyper-V-Management-PowerShell
            if ($hv.RestartNeeded) {
                Write-Host "Restarting..." -ForegroundColor Yellow
                Restart-Computer -Force
            }
        }
        Start-Sleep -Seconds 5
        & { minikube start -p $script:Name --cpus=4 --memory=8192 --nodes=4 --driver=hyperv }
        if (-not $?) {
            Write-Host "Error creating minikube cluster - $($errOut)" -ForegroundColor Red
            minikube logs --file=$($script:Name).log
            exit -1
        }
        Write-Host "Cluster created..." -ForegroundColor Green
    }
}
elseif ($script:ClusterType -eq "kind") {
    $errOut = $($clusters = & { kind get clusters } -split "`n") 2>&1
    if (($clusters -contains $script:Name) -and (!$forceReinstall)) {
        Write-Host "Cluster $script:Name exists..." -ForegroundColor Green
    }
    elseif (-not $?) {
        Write-Host "Error querying kind clusters - $($errOut)" -ForegroundColor Red
        exit -1
    }
    else {
        foreach ($cluster in $clusters) {
            if (!$forceReinstall) {
                if ($(Read-Host "Delete existing cluster $cluster? [Y/N]") -ne "Y") {
                    continue
                }
            }
            Write-Host "Deleting existing cluster $cluster..." -ForegroundColor Yellow
            kind delete cluster --name $cluster 2>&1 | Out-Null
        }
        Write-Host "Creating kind cluster $script:Name..." -ForegroundColor Cyan

        $clusterConfig = @"
kind: Cluster
apiVersion: kind.x-k8s.io/v1alpha4
nodes:
- role: control-plane
  extraPortMappings:
  - containerPort: 80
    hostPort: 80
    listenAddress: "127.0.0.1"
- role: worker
- role: worker
- role: worker
- role: worker
- role: worker
"@
        $clusterConfig -replace "`r`n", "`n" `
        | kind create cluster --name $script:Name --config -
        if (-not $?) {
            Write-Host "Error creating kind cluster - $($errOut)" -ForegroundColor Red
            exit -1
        }
        Write-Host "Cluster created..." -ForegroundColor Green
    }
}
else {
    Write-Host "Error: Unsupported cluster type $script:ClusterType" -ForegroundColor Red
    exit -1
}

$errOut = $($stdOut = & { kubectl get nodes }) 2>&1
if (-not $?) {
    $stdOut | Out-Host
    Write-Host "Cluster not reachable : $errOut" -ForegroundColor Red
    exit -1
}
$stdOut | Out-Host

Write-Host "Registering the required resource providers..." -ForegroundColor Cyan
$resourceProviders =
@(
    "Microsoft.ExtendedLocation",
    "Microsoft.Kubernetes",
    "Microsoft.KubernetesConfiguration",
    "Microsoft.EventGrid",
    "Microsoft.EventHub",
    "Microsoft.KeyVault",
    "Microsoft.Storage",
    "Microsoft.IoTOperations",
    "Microsoft.Kusto"
)
foreach ($rp in $resourceProviders) {
    $errOut = $($obj = & { az provider show -n $rp `
                --subscription $SubscriptionId `
                --only-show-errors --output json | ConvertFrom-Json }) 2>&1
    if (-not $?) {
        Write-Host "Error querying provider $rp : $errOut" -ForegroundColor Red
        exit -1
    }
    if ($obj.registrationState -eq "Registered") {
        continue
    }
    $errOut = $($retVal = & { az provider register -n `
                $rp --subscription $SubscriptionId --wait }) 2>&1
    if (-not $?) {
        $retVal | Out-Host
        Write-Host "Error registering provider $rp : $errOut" -ForegroundColor Red
        exit -1
    }
    Write-Host "Resource provider $rp registered." -ForegroundColor Green
}

$errOut = $($rg = & { az group show `
    --name $script:ResourceGroup `
    --subscription $script:SubscriptionId `
    --only-show-errors --output json } | ConvertFrom-Json) 2>&1
if ($rg -and $forceReinstall) {
    Write-Host "Deleting existing resource group $($rg.Name)..." `
        -ForegroundColor Yellow
    az group delete --name $rg.Name --subscription $SubscriptionId `
        --only-show-errors --yes
    $rg = $null
}
if (!$rg) {
    Write-Host "Creating resource group $script:ResourceGroup..." -ForegroundColor Cyan
    $errOut = $($rg = & { az group create `
        --name $script:ResourceGroup `
        --location $script:Location `
        --subscription $script:SubscriptionId `
        --only-show-errors --output json } | ConvertFrom-Json) 2>&1
    if (-not $? -or !$rg) {
        Write-Host "Error creating resource group - $($errOut)." -ForegroundColor Red
        exit -1
    }
    Write-Host "Resource group $($rg.id) created." -ForegroundColor Green
}
else {
    Write-Host "Resource group $($rg.id) exists." -ForegroundColor Green
}

#
# Create Azure resources
#

# Managed identity
$errOut = $($mi = & { az identity show `
    --name $script:Name `
    --resource-group $($rg.Name) `
    --subscription $SubscriptionId `
    --only-show-errors --output json } | ConvertFrom-Json) 2>&1
if (!$mi) {
    Write-Host "Creating managed identity $script:Name..." -ForegroundColor Cyan
    $errOut = $($mi = & { az identity create `
        --name $script:Name `
        --location $Location `
        --resource-group $($rg.Name) `
        --subscription $SubscriptionId `
        --only-show-errors --output json } | ConvertFrom-Json) 2>&1
    if (-not $? -or !$mi) {
        Write-Host "Error creating managed identity - $($errOut)." -ForegroundColor Red
        exit -1
    }
    Write-Host "Managed identity $($mi.id) created." -ForegroundColor Green
}
else {
    Write-Host "Managed identity $($mi.id) exists." -ForegroundColor Green
}

# Storage account
$storageAccountName = $script:Name.Replace("-", "")
$errOut = $($stg = & { az storage account show `
    --name $storageAccountName `
    --resource-group $($rg.Name) `
    --subscription $SubscriptionId `
    --only-show-errors --output json } | ConvertFrom-Json) 2>&1
if (!$stg) {
    Write-Host "Creating Storage account $storageAccountName" -ForegroundColor Cyan
    $errOut = $($stg = & { az storage account create `
        --name $storageAccountName `
        --location $Location `
        --resource-group $($rg.Name) `
        --allow-shared-key-access false `
        --enable-hierarchical-namespace `
        --subscription $SubscriptionId `
        --only-show-errors --output json } | ConvertFrom-Json) 2>&1
    if (-not $? -or !$stg) {
        Write-Host "Error creating storage $storageAccountName - $($errOut)." `
            -ForegroundColor Red
        exit -1
    }
    Write-Host "Storage account $($stg.id) created." -ForegroundColor Green
}
else {
    Write-Host "Storage account $($stg.id) exists." -ForegroundColor Green
}

# Event Hub namespace
$eventHubNamespace = $script:Name
$errOut = $($ehNs = & { az eventhubs namespace show `
    --name $eventHubNamespace `
    --resource-group $($rg.Name) `
    --subscription $SubscriptionId `
    --only-show-errors --output json } | ConvertFrom-Json) 2>&1
if (!$ehNs) {
    Write-Host "Creating Event Hub Namespace $($eventHubNamespace)..." -ForegroundColor Cyan
    $errOut = $($ehNs = & { az eventhubs namespace create `
        --name $eventHubNamespace `
        --resource-group $rg.Name `
        --location $Location `
        --disable-local-auth true `
        --subscription $SubscriptionId `
        --only-show-errors --output json } | ConvertFrom-Json) 2>&1
    if (-not $? -or !$ehNs) {
        Write-Host "Error creating Event Hub Namespace $($eventHubNamespace) - $($errOut)." `
            -ForegroundColor Red
        exit -1
    }
    Write-Host "Event Hub Namespace $($ehNs.id) created." -ForegroundColor Green
}
else {
    Write-Host "Event Hub Namespace $($ehNs.id) exists." -ForegroundColor Green
}

# Keyvault
$keyVaultName = $script:Name + "kv"
$errOut = $($kv = & { az keyvault show `
    --name $keyVaultName `
    --subscription $SubscriptionId `
    --only-show-errors --output json } | ConvertFrom-Json) 2>&1
if (!$kv) {
    Write-Host "Creating Key vault $keyVaultName" -ForegroundColor Cyan
    az keyvault purge --name $keyVaultName --location $Location `
        --subscription $SubscriptionId 2>&1 | Out-Null
    $errOut = $($kv = & { az keyvault create `
        --enable-rbac-authorization true `
        --name $keyVaultName `
        --resource-group $($rg.Name) `
        --subscription $SubscriptionId `
        --only-show-errors --output json } | ConvertFrom-Json) 2>&1
    if (-not $? -or !$kv) {
        Write-Host "Error creating Azure Keyvault - $($errOut)." `
            -ForegroundColor Red
        exit -1
    }
    if ($kv.properties.enableSoftDelete) {
        az keyvault update `
            --name $keyVaultName `
            --enable-soft-delete false `
            --resource-group $($rg.Name) `
            --subscription $SubscriptionId 2>&1 | Out-Null
    }
    Write-Host "Key vault $($kv.id) created..." -ForegroundColor Green
}
else {
    Write-Host "Key vault $($kv.id) exists." -ForegroundColor Green
}

# Azure IoT Operations schema registry
$srName = "$($script:Name.ToLowerInvariant())sr"
$errOut = $($sr = & { az iot ops schema registry show `
    --name $srName `
    --resource-group $($rg.Name) `
    --subscription $SubscriptionId `
    --only-show-errors --output json } | ConvertFrom-Json) 2>&1
if (!$sr) {
    Write-Host "Creating Azure IoT Operations schema registry..." -ForegroundColor Cyan
    $errOut = $($sr = & { az iot ops schema registry create `
        --name $srName `
        --resource-group $($rg.Name) `
        --registry-namespace $srName `
        --location $Location `
        --sa-resource-id $stg.id `
        --subscription $SubscriptionId `
        --only-show-errors --output json } | ConvertFrom-Json) 2>&1
    if (-not $? -or !$sr) {
        Write-Host "Error creating Azure IoT Operations schema registry - $($errOut)." `
            -ForegroundColor Red
        exit -1
    }
    Write-Host "Azure IoT Operations schema registry $($sr.id) created." `
        -ForegroundColor Green
}
else {
    Write-Host "Azure IoT Operations schema registry $($sr.id) exists." `
        -ForegroundColor Green
}

#
# Create adr namespace
#
$adrNsResource = "/subscriptions/$($SubscriptionId)"
$adrNsResource = "$($adrNsResource)/resourceGroups/$($rg.Name)"
$adrNsResource = "$($adrNsResource)/providers/Microsoft.DeviceRegistry/namespaces/$($script:Name)"
$errOut = $($ns = & { az rest --method get `
    --url "$($adrNsResource)?api-version=2025-10-01" `
    --headers "Content-Type=application/json" } | ConvertFrom-Json) 2>&1
if (!$ns -or !$ns.id) {
    $body = @{
        location = $Location
        identity = @{
            type = "SystemAssigned"
        }
        properties = @{}
    } | ConvertTo-Json -Depth 100
    $tempFile = New-TemporaryFile
    $body | Out-File -FilePath $tempFile -Encoding utf8 -Force
    Write-Host "Creating ADR namespace $adrNsResource..." -ForegroundColor Cyan
    $errOut = $($ns = & { az rest --method put `
        --url "$($adrNsResource)?api-version=2025-10-01" `
        --headers "Content-Type=application/json" `
        --body @$tempFile } | ConvertFrom-Json) 2>&1
    if (-not $?) {
        Write-Host "Error: Failed to create ADR namespace $($adrNsResource) - $($errOut)." `
            -ForegroundColor Red
        Remove-Item $tempFile -Force
        exit -1
    }
    Remove-Item $tempFile -Force
    Write-Host "ADR namespace $($ns.id) created." -ForegroundColor Green
}
else {
    Write-Host "ADR namespace $($ns.id) exists." -ForegroundColor Green
}

#
# Assign roles to the managed identity
#
$roleAssignments =
@(
    @("Contributor", $stg.id, $mi.principalId),
    @("Storage Blob Data Owner", $stg.id, $mi.principalId),
    @("Contributor", $ehNs.id, $mi.principalId),
    @("Contributor", $ns.id, $mi.principalId),
    @("Contributor", $sr.id, $mi.principalId),
    @("Contributor", $rg.id, $mi.principalId),
    @("Key Vault Administrator", $kv.id, $mi.principalId)
)
foreach ($ra in $roleAssignments) {
    Write-Host "Assigning $($ra[0]) role to $($ra[2])..." -ForegroundColor Cyan
    $errOut = $($obj = & { az role assignment create `
        --role $ra[0] `
        --assignee-object-id $ra[2] `
        --assignee-principal-type ServicePrincipal `
        --scope $ra[1] | ConvertFrom-Json }) 2>&1
    if (-not $?) {
        Write-Host "Error assigning role $($ra[0]) to $($ra[2]) : $errOut" `
            -ForegroundColor Red
        #exit -1
    }
    Write-Host "Role $($ra[0]) assigned to $($ra[2])." -ForegroundColor Green
}

#
# Connect the cluster to Arc
#
$errOut = $($cc = & { az connectedk8s show `
    --name $script:Name `
    --resource-group $($rg.Name) `
    --subscription $SubscriptionId `
    --only-show-errors --output json } | ConvertFrom-Json) 2>&1
if ($cc -and !$forceReinstall -and $cc.properties.connectivityStatus -ne "Offline") {
    Write-Host "Cluster $($cc.name) already connected to Arc." -ForegroundColor Green
}
else {
    if ($cc) {
        Write-Host "Disconnecting existing Arc cluster $($cc.name)..." `
            -ForegroundColor Yellow
        az connectedk8s delete --only-show-errors `
            --name $cc.name `
            --resource-group $($rg.Name) `
            --subscription $SubscriptionId `
            --yes 2>&1 | Out-Null
        if (-not $?) {
            Write-Host "Error: disconnecting cluster $($cc.name) from Arc failed." `
                -ForegroundColor Red
            exit -1
        }
    }
    Write-Host "Connecting cluster to Arc in $($rg.Name)..." -ForegroundColor Cyan
    az connectedk8s connect --only-show-errors `
        --name $script:Name `
        --resource-group $($rg.Name) `
        --subscription $SubscriptionId `
        --correlation-id "d009f5dd-dba8-4ac7-bac9-b54ef3a6671a" 2>&1 | Out-Host
    if (-not $?) {
        Write-Host "Error: connecting cluster $($script:Name) to Arc failed." -ForegroundColor Red
        exit -1
    }
    $errOut = $($cc = & { az connectedk8s show `
        --name $script:Name `
        --resource-group $($rg.Name) `
        --subscription $SubscriptionId `
        --only-show-errors --output json } | ConvertFrom-Json) 2>&1
    Write-Host "Cluster $($cc.name) connected to Arc." -ForegroundColor Green
}

# enable custom location feature
$errOut = $($objectId = & { az ad sp show `
            --id bc313c14-388c-4e7d-a58e-70017303ee3b --query id -o tsv }) 2>&1
Write-Host "Enabling custom location feature for cluster $script:Name..." -ForegroundColor Cyan
az connectedk8s enable-features `
    --name $cc.name `
    --resource-group $rg.Name `
    --subscription $SubscriptionId `
    --custom-locations-oid $objectId `
    --features cluster-connect custom-locations `
    --only-show-errors
if (-not $?) {
    Write-Host "Error: Failed to enable custom location feature." -ForegroundColor Red
    exit -1
}

# enable workload identity
if ($cc.securityProfile.workloadIdentity.enabled -and $cc.oidcIssuerProfile.enabled){
    Write-Host "Workload identity already enabled for cluster $script:Name." -ForegroundColor Green
}
else {
    Write-Host "Enabling workload identity for cluster $script:Name..." -ForegroundColor Cyan
    az connectedk8s update `
        --name $cc.name `
        --resource-group $rg.Name `
        --subscription $SubscriptionId `
        --auto-upgrade true `
        --enable-oidc-issuer `
        --enable-workload-identity `
        --only-show-errors
    if (-not $?) {
        Write-Host "Error: Failed to enable workload identity." -ForegroundColor Red
        exit -1
    }
    Write-Host "Workload identity enabled for cluster $script:Name." -ForegroundColor Green
}

# Validate we have a connected cluster
while ($true) {
    $errOut = $($cc = & { az connectedk8s show `
        --name $script:Name `
        --resource-group $($rg.Name) `
        --subscription $SubscriptionId `
        --only-show-errors --output json } | ConvertFrom-Json) 2>&1
    if ($cc.properties.connectivityStatus -eq "Connected") {
        Write-Host "Cluster $($cc.name) connected to Arc." -ForegroundColor Green
        break
    }
    Write-Host "Waiting for cluster $($cc.name) to connect - $($cc.properties.connectivityStatus)..." `
        -ForegroundColor Yellow
    Start-Sleep -Seconds 10
}

#
# Create Azure IoT Operations instance
#
$errOut = $($iotOps = & { az iot ops show `
    --resource-group $rg.Name `
    --name $script:OpsInstanceName `
    --subscription $SubscriptionId `
    --only-show-errors --output json } | ConvertFrom-Json) 2>&1
if ($iotOps) {
    # first check cluster extension is deployed on the cluster and if not reset the instance
    $queryVersion = "[?scope.cluster.releaseNamespace == '$($script:ClusterNamespace)'"
    $queryVersion = "$($queryVersion) && extensionType == 'microsoft.iotoperations'"
    $queryVersion = "$($queryVersion) ].{ version:currentVersion, train:releaseTrain }"
    $currentVersion = $(az k8s-extension list `
        --cluster-name $script:Name --cluster-type connectedClusters `
        --resource-group $rg.Name `
        --query $queryVersion --output json) | ConvertFrom-Json
    if (!$currentVersion) {
        Write-Host "No Azure IoT Operations extension found on cluster $script:Name. Resetting." `
            - ForegroundColor Yellow
        az iot ops delete --name $iotOps.name --cluster $script:Name --yes --only-show-errors `
            --resource-group $rg.Name --subscription $SubscriptionId `
        if (-not $?) {
            Write-Host "Error removing Azure IoT Operations instance from cluster $script:Name." `
                -ForegroundColor Red
            exit -1
        }
        Write-Host "Azure IoT Operations instance removed from cluster $script:Name." `
            -ForegroundColor Green
        $iotOps = $null
        $currentVersion = $null
    }
    else {
        Write-Host "Azure IoT Operations instance found with $($currentVersion.Version)." `
            -ForegroundColor Green
    }
}
if (!$iotOps) {
    Write-Host "Initializing cluster $script:Name for deployment of Azure IoT operations..." `
        -ForegroundColor Cyan
    $iotOpsInit = @(
        "init", `
        "--cluster", $script:Name, `
        "--resource-group", $rg.Name, `
        "--subscription", $SubscriptionId, `
        "--ensure-latest", $ensureLatest, `
        "--only-show-errors"
    )
    & az iot ops $iotOpsInit
    if (-not $?) {
        Write-Host "Error initializing cluster $script:Name for Azure IoT Operations." `
            -ForegroundColor Red
        exit -1
    }
    Write-Host "Cluster ready for Azure IoT Operations deployment..." `
        -ForegroundColor Green
    Write-Host "Creating the Azure IoT Operations instance $($script:OpsInstanceName)..." `
        -ForegroundColor Cyan
    $iotOpsCreate = @(
        "create", `
        "--cluster", $script:Name, `
        "--resource-group", $rg.Name, `
        "--subscription", $SubscriptionId, `
        "--cluster-namespace", $script:ClusterNamespace, `
        "--name", $script:OpsInstanceName, `
        "--location", $Location, `
        "--sr-resource-id", $sr.id, `
        "--ns-resource-id", $ns.id, `
        "--add-insecure-listener", "true", `
        "--only-show-errors"
    )
    if ($script:OpsVersion) {
        $iotOpsCreate += "--ops-train", $script:OpsTrain
        $iotOpsCreate += "--ops-version", $script:OpsVersion
    }
    & az iot ops $iotOpsCreate
    if (-not $?) {
        & az iot ops $iotOpsCreate
        if (-not $?) {
            Write-Host "Error creating Azure IoT Operations instance - $($errOut)." `
                -ForegroundColor Red
            exit -1
        }
    }
    Write-Host "Enabling Azure IoT Operations resource sync..." `
        -ForegroundColor Cyan
    $errOut = $(az iot ops enable-rsync `
        --resource-group $($rg.Name) `
        --name $script:OpsInstanceName `
        --subscription $SubscriptionId `
        --only-show-errors) 2>&1
    if (-not $?) {
        Write-Host "Error enabling Azure IoT Operations resource sync - $($errOut)." `
            -ForegroundColor Red
        exit -1
    }
    Write-Host "Azure IoT Operations resource sync enabled." `
        -ForegroundColor Green

    $errOut = $($iotOps = & { az iot ops show `
        --resource-group $($rg.Name) `
        --name $script:OpsInstanceName `
        --subscription $SubscriptionId `
        --only-show-errors --output json } | ConvertFrom-Json) 2>&1
    if (-not $iotOps) {
        Write-Host "Error retrieving created Azure IoT Operations instance - $($errOut)." `
            -ForegroundColor Red
        exit -1
    }
    Write-Host "Azure IoT Operations instance $($iotOps.id) created." `
        -ForegroundColor Green
}
else {
    # check if we need to upgrade the cluster extension
    $upgrade = $true
    if ($script:OpsVersion -and $currentVersion.Version -ge $script:OpsVersion) {
        $upgrade = $false
        if ($script:OpsTrain -and $script:OpsTrain -ne $currentVersion.Train) {
            $upgrade = $true
        }
    }
    if ($upgrade) {
        Write-Host "Upgrading Azure IoT Operations $($iotOps.id) $($currentVersion.Version)..." `
            -ForegroundColor Cyan
        $iotOpsUpgrade = @(
            "upgrade", `
            "--name", $iotOps.name, `
            "--resource-group", $rg.Name, `
            "--subscription", $SubscriptionId, `
            "--only-show-errors",
            "--yes"
        )
        if ($script:OpsVersion) {
            $iotOpsUpgrade += "--ops-train", $script:OpsTrain
            $iotOpsUpgrade += "--ops-version", $script:OpsVersion
        }
        & az iot ops $iotOpsUpgrade
        if (-not $?) {
            Write-Host "Error upgrading Azure IoT Operations instance - $($errOut)." `
                -ForegroundColor Red
            exit -1
        }
        Write-Host "Azure IoT Operations $($iotOps.id) upgraded ..." `
            -ForegroundColor Green
    }
    else {
        Write-Host "Azure IoT Operations $($iotOps.id) at version $($currentVersion.Version)." `
            -ForegroundColor Green
    }
}

$errOut = $($mia = & { az iot ops identity show `
    --name $iotOps.name `
    --resource-group $($rg.Name) `
    --subscription $SubscriptionId `
    --only-show-errors --output json } | ConvertFrom-Json) 2>&1
if (!$mia -or $mia.type -ne "UserAssigned") {
    Write-Host "Assign managed identity $($mi.id) to instance $($iotOps.id)..." `
        -ForegroundColor Cyan
    $errOut = $($mia = & { az iot ops identity assign `
        --name $iotOps.name `
        --resource-group $($rg.Name) `
        --subscription $SubscriptionId `
        --mi-user-assigned $mi.id `
        --only-show-errors --output json } | ConvertFrom-Json) 2>&1
    if (-not $? -or !$mia) {
        Write-Host "Error assigning managed identity to instance - $($errOut)." `
            -ForegroundColor Red
        exit -1
    }
    Write-Host "Managed identity $($mi.id) assigned to instance $($iotOps.id)." `
        -ForegroundColor Green
}
else {
    Write-Host "Managed identity $($mi.id) already assigned to $($iotOps.id)." `
        -ForegroundColor Green
}

$errOut = $($ss = & { az iot ops secretsync list `
    --instance $iotOps.name `
    --resource-group $($rg.Name) `
    --subscription $SubscriptionId `
    --only-show-errors --output json } | ConvertFrom-Json) 2>&1
if (!$ss) {
    Write-Host "Enabling secret sync with $($kv.id) for instance $($iotOps.id)..." `
        -ForegroundColor Cyan
    $errOut = $($ss = & { az iot ops secretsync enable `
        --instance $iotOps.name `
        --kv-resource-id $kv.id `
        --resource-group $rg.Name `
        --subscription $SubscriptionId `
        --mi-user-assigned $mi.id `
        --only-show-errors --output json } | ConvertFrom-Json) 2>&1
    if (-not $? -or !$ss) {
        Write-Host "Error enabling secret sync for instance - $($errOut)." `
            -ForegroundColor Red
        exit -1
    }
    Write-Host "Secret sync with $($kv.id) enabled for $($iotOps.id)." `
        -ForegroundColor Green
}
else {
    Write-Host "Secret sync with $($kv.id) already enabled in $($iotOps.id)." `
        -ForegroundColor Green
}

$eventHubEndpointName = "eventhub-endpoint"
$errOut = $($eh = & { az iot ops dataflow endpoint show `
    --instance $iotOps.name `
    --name $eventHubEndpointName `
    --resource-group $rg.Name `
    --subscription $SubscriptionId `
    --only-show-errors --output json } | ConvertFrom-Json) 2>&1
if (!$eh) {
    Write-Host "Creating Event Hub endpoint $($eventHubEndpointName) in $($iotOps.id)..." `
        -ForegroundColor Cyan
    $errOut = $($eh = & { az iot ops dataflow endpoint create eventhub `
        --name $eventHubEndpointName `
        --instance $iotOps.name `
        --resource-group $rg.Name `
        --eventhub-namespace $ehNs.Name `
        --subscription $SubscriptionId `
        --only-show-errors --output json } | ConvertFrom-Json) 2>&1
    if (-not $? -or !$ss) {
        Write-Host "Error creating Event Hub endpoint $($eventHubEndpointName) - $($errOut)." `
            -ForegroundColor Red
        exit -1
    }
    Write-Host "Event Hub endpoint $($dp.id) created in $($iotOps.id)." `
        -ForegroundColor Green
}
else {
    Write-Host "Event Hub endpoint $($eh.id) exists in $($iotOps.id)." `
        -ForegroundColor Green
}

#
# Deploy opc publisher as connector
#
if ($script:Connector -ne "None") {
    $discoveryMode = "Off"
    if ($script:EnableNetworkDiscovery.IsPresent) {
        $discoveryMode = "Fast"
    }
    & $(Join-Path $scriptDirectory "deploy.ps1") `
        -Name $script:Name `
        -ClusterType $script:ClusterType `
        -ConnectorType $script:Connector `
        -OpsInstanceName $iotOps.Name `
        -AdrNamespaceName $ns.Name `
        -SubscriptionId $SubscriptionId `
        -TenantId $TenantId `
        -ResourceGroup $rg.Name `
        -Location $Location `
        -NetworkDiscoveryMode $discoveryMode `
        -SkipLogin `
        -Force  # :$forceReinstall `

    if (-not $?) {
        Write-Host "Error deploying opc publisher connector." -ForegroundColor Red
        exit -1
    }
}
else {
    Write-Host "No connector selected - no connector will be deployed..." `
        -ForegroundColor Yellow
}

#
# Deploy simulation servers
#
if ($script:DeployPlcSimulation.IsPresent) {
    & $(Join-Path $(Join-Path $scriptDirectory "simulation") "deploy.ps1") `
        -DeploymentName "simulation1" `
        -SimulationName "opc-plc" -Count 2 `
        -InstanceName $script:OpsInstanceName `
        -AdrNamespaceName $script:Name `
        -SubscriptionId $SubscriptionId `
        -TenantId $TenantId `
        -ResourceGroup $rg.Name `
        -Location $Location `
        -Namespace $script:ClusterNamespace `
        -SkipLogin `
        -Force  # :$forceReinstall `

    if (-not $?) {
        Write-Host "Error deploying opc plc simulation servers." -ForegroundColor Red
        exit -1
    }
}
if ($script:DeployTestSimulation.IsPresent) {
    & $(Join-Path $(Join-Path $scriptDirectory "simulation") "deploy.ps1") `
        -DeploymentName "simulation2" `
        -SimulationName "opc-test" -Count 2 `
        -InstanceName $script:OpsInstanceName `
        -AdrNamespaceName $script:Name `
        -SubscriptionId $SubscriptionId `
        -TenantId $TenantId `
        -ResourceGroup $rg.Name `
        -Location $Location `
        -Namespace $script:ClusterNamespace `
        -SkipLogin `
        -Force  # :$forceReinstall `

    if (-not $?) {
        Write-Host "Error deploying test simulation servers." -ForegroundColor Red
        exit -1
    }
}

