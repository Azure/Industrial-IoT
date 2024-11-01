
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
   .PARAMETER TenantId
      The tenant id to use when logging into Azure.
   .PARAMETER SubscriptionId
      The subscription id to scope all activity to.
   .PARAMETER Location
      The location of the cluster.
   .PARAMETER ClusterType
      The type of cluster to create. Default is kind.
   .PARAMETER Force
      Force reinstall.
#>

param(
    [string] [Parameter(Mandatory = $true)] $Name,
    [string] $ResourceGroup,
    [string] $TenantId,
    [string] $SubscriptionId,
    [string] $Location,
    [string] [ValidateSet("kind", "minikube", "k3d")] $ClusterType = "minikube",
    [switch] $Force
)

$forceReinstall = $Force.IsPresent
$forceReinstall = $true
$TenantId = "6e54c408-5edd-4f87-b3bb-360788b7ca18"

#Requires -RunAsAdministrator

$ErrorActionPreference = 'Stop'
# $path = Split-Path $script:MyInvocation.MyCommand.Path

if (! [Environment]::Is64BitProcess) {
   Write-Host "Error: Run this in 64bit Powershell session" -ForegroundColor Red
   exit -1
}

if ([string]::IsNullOrWhiteSpace($ResourceGroup)) {
   $ResourceGroup = $Name
}
if ([string]::IsNullOrWhiteSpace($TenantId)) {
   $TenantId = $env:AZURE_TENANT_ID
}
if ([string]::IsNullOrWhiteSpace($Location)) {
   $Location = "eastus2"
}

Write-Host "Ensuring all required dependencies are installed..." -ForegroundColor Cyan
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$errOut = $($docker = & {docker version --format json | ConvertFrom-Json}) 2>&1
if ($LASTEXITCODE -ne 0 -or !$docker.Server) {
    $docker | Out-Host
    throw "Docker not installed or running : $errOut"
}
Write-Host "Found $($docker.Server.Platform.Name)..." -ForegroundColor Green
$installAz = $false
try {
    $azVersion = (az version)[1].Split(":")[1].Split('"')[1]
    if ($azVersion -lt "2.64.0" -or !$azVersion) {
        $installAz = $true
    }
}
catch {
    $installAz = $true
}

# install az
if ($installAz) {
    Write-Host "Installing Az CLI..." -ForegroundColor Cyan

    Set-ExecutionPolicy Bypass -Scope Process -Force
    $ProgressPreference = 'SilentlyContinue'
    Invoke-WebRequest -Uri https://aka.ms/installazurecliwindowsx64 -OutFile .\AzureCLI.msi
    Start-Process msiexec.exe -Wait -ArgumentList '/I AzureCLI.msi /quiet'
    Remove-Item .\AzureCLI.msi
}

# install required az extensions
az config set extension.dynamic_install_allow_preview=true 2>&1 | Out-Null
$extensions  =
@(
    "connectedk8s",
    "azure-iot-ops",
    "k8s-configuration"
)
foreach ($p in $extensions) {
    $errOut = $($stdout = & {az extension add --upgrade --name $p}) 2>&1
    if ($LASTEXITCODE -ne 0) {
        $stdout | Out-Host
        throw "Error installing az extension $p : $errOut"
    }
}
if (![string]::IsNullOrWhiteSpace($SubscriptionId)) {
    az account set --subscription $SubscriptionId 2>&1 | Out-Null
}

# install choco
$errout = $($version = & {choco --version}) 2>&1
if (!$version -or $errout) {
    Write-Host "Installing Choco CLI..." -ForegroundColor Cyan
    [System.Net.ServicePointManager]::SecurityProtocol = `
        [System.Net.ServicePointManager]::SecurityProtocol -bor 3072
    $scriptLoc = 'https://community.chocolatey.org/install.ps1'
    Invoke-Expression ((New-Object System.Net.WebClient).DownloadString($scriptLoc))
}

# install kind, helm and kubectl
$packages  =
@(
    "kubernetes-cli",
    "kubernetes-helm",
    "k9s",
    $ClusterType
)
foreach($p in $packages) {
    $errOut = $($stdout = & {choco install $p --yes}) 2>&1
    if ($LASTEXITCODE -ne 0) {
        $stdout | Out-Host
        throw "Error choco installing package $p : $errOut"
    }
}

#
# Log into azure
#
Write-Host "Log into Azure..." -ForegroundColor Cyan
$loginparams = @()
if (![string]::IsNullOrWhiteSpace($TenantId)) {
    $loginparams += @("--tenant", $TenantId)
}
$session = (az login @loginparams) | ConvertFrom-Json
if (-not $session) {
    Write-Host "Error: Login failed." -ForegroundColor Red
    exit -1
}
$SubscriptionId = $session.id
$TenantId = $session.tenantId

#
# Create the cluster
#
if ($ClusterType -eq "k3d") {
    $errOut = $($table = & {k3d cluster list --no-headers} -split "`n") 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Error querying k3d clusters - $errOut" -ForegroundColor Red
        exit -1
    }
    $clusters = $table | ForEach-Object { $($_ -split " ")[0].Trim() }
    if (($clusters -contains $Name) -and (!$forceReinstall)) {
        Write-Host "Cluster $Name exists..." -ForegroundColor Green
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
        Write-Host "Creating k3d cluster $Name..." -ForegroundColor Cyan

        k3d cluster create $Name `
            --agents 4 `
            --agents-memory 4m `
            --servers 1 `
            --servers-memory 8m `
            --wait
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Error creating k3d cluster - $errOut" -ForegroundColor Red
            exit -1
        }
        Write-Host "Cluster created..." -ForegroundColor Green
    }
}
elseif ($ClusterType -eq "minikube") {
    $errOut = $($clusters = & {minikube profile list -o json} | ConvertFrom-Json) 2>&1
    if (($clusters.valid.Name -contains $Name) -and (!$forceReinstall)) {
        Write-Host "Valid minikube cluster $Name exists..." -ForegroundColor Green
        # Start the cluster
        if ($stat.Host -Contains "Stopped" -or
            $stat.APIServer -Contains "Stopped"-or
            $stat.Kubelet -Contains "Stopped") {
            Write-Host "Minikube cluster $Name stopped. Starting..." -ForegroundColor Cyan
            minikube start -p $Name
            if ($LASTEXITCODE -ne 0) {
                Write-Host "Error starting minikube cluster." -ForegroundColor Red
                minikube logs --file=$($Name).log
                exit -1
            }
            Write-Host "Minikube cluster $Name started." -ForegroundColor Green
        }
        else {
            Write-Host "Minikube cluster $Name running." -ForegroundColor Green
        }
    }
    elseif ($LASTEXITCODE -ne 0) {
        Write-Host "Error querying minikube clusters - $errOut" -ForegroundColor Red
        exit -1
    }
    else {
        if ($forceReinstall) {
            Write-Host "Deleting other clusters..." -ForegroundColor Yellow
            minikube delete --all --purge
        }
        elseif ($clusters.invalid.Name -contains $Name) {
            Write-Host "Delete bad minikube cluster $Name..." -ForegroundColor Yellow
            minikube delete -p $Name
        }
        Write-Host "Creating new minikube cluster $Name..." -ForegroundColor Cyan

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

        & {minikube start -p $Name --cpus=4 --memory=4096 --nodes=4 --driver=hyperv}
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Error creating minikube cluster - $errOut" -ForegroundColor Red
            minikube logs --file=$($Name).log
            exit -1
        }
        Write-Host "Cluster created..." -ForegroundColor Green
    }
}
elseif ($ClusterType -eq "kind") {
    $errOut = $($clusters = & {kind get clusters} -split "`n") 2>&1
    if (($clusters -contains $Name) -and (!$forceReinstall)) {
        Write-Host "Cluster $Name exists..." -ForegroundColor Green
    }
    elseif ($LASTEXITCODE -ne 0) {
        Write-Host "Error querying kind clusters - $errOut" -ForegroundColor Red
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
        Write-Host "Creating kind cluster $Name..." -ForegroundColor Cyan

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
            | kind create cluster --name $Name --config -
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Error creating kind cluster - $errOut" -ForegroundColor Red
            exit -1
        }
        Write-Host "Cluster created..." -ForegroundColor Green
    }
}
else {
    Write-Host "Error: Unsupported cluster type $ClusterType" -ForegroundColor Red
    exit -1
}
$errOut = $($stdout = & {kubectl get nodes}) 2>&1
if ($LASTEXITCODE -ne 0) {
    $stdout | Out-Host
    throw "Cluster not reachable : $errOut"
}
$stdout | Out-Host


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
    $errOut = $($obj = & {az provider show -n $rp `
        --subscription $SubscriptionId | ConvertFrom-Json}) 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Error querying provider $rp : $errOut"
    }
    if ($obj.registrationState -eq "Registered") {
        continue
    }
    $errOut = $($retVal = & {az provider register -n `
        $rp --subscription $SubscriptionId}) 2>&1
    if ($LASTEXITCODE -ne 0) {
        $retVal | Out-Host
        throw "Error registering provider $rp : $errOut"
    }
    Write-Host "Resource provider $p registered." -ForegroundColor Green
}

$errOut = $($rg = & {az group show `
    --name $srName `
    --resource-group $ResourceGroup `
    --subscription $SubscriptionId} | ConvertFrom-Json) 2>&1
if ($rg -and $forceReinstall) {
    Write-Host "Deleting existing resource group $Name..." `
        -ForegroundColor Yellow
    az group delete --name $Name --subscription $SubscriptionId `
        --yes 2>&1 | Out-Null
    $rg = $null
}
if (!$rg) {
    Write-Host "Creating resource group $Name..." -ForegroundColor Cyan
    $errOut = $($rg = & {az group create `
        --name $Name `
        --location $Location `
        --subscription $SubscriptionId} | ConvertFrom-Json) 2>&1
    if ($LASTEXITCODE -ne 0 -or !$rg) {
        Write-Host "Error creating resource group - $errOut." -ForegroundColor Red
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
$errOut = $($mi = & {az identity show `
    --name $Name `
    --resource-group $ResourceGroup `
    --subscription $SubscriptionId} | ConvertFrom-Json) 2>&1
if (!$mi) {
    Write-Host "Creating managed identity $Name..." -ForegroundColor Cyan
    $errOut = $($mi = & {az identity create `
        --name $Name `
        --location $Location `
        --resource-group $ResourceGroup `
        --subscription $SubscriptionId} | ConvertFrom-Json) 2>&1
    if ($LASTEXITCODE -ne 0 -or !$mi) {
        Write-Host "Error creating managed identity - $errOut." -ForegroundColor Red
        exit -1
    }
    Write-Host "Managed identity $($mi.id) created." -ForegroundColor Green
}
else {
    Write-Host "Managed identity $($mi.id) exists." -ForegroundColor Green
}

# Storage account
$storageAccountName = $Name.Replace("-", "")
$errOut = $($stg = & {az storage account show `
    --name $storageAccountName `
    --resource-group $ResourceGroup `
    --subscription $SubscriptionId} | ConvertFrom-Json) 2>&1
if (!$stg) {
    Write-Host "Creating Storage account $storageAccountName" -ForegroundColor Cyan
    $errOut = $($stg = & {az storage account create `
        --name $storageAccountName `
        --location $Location `
        --resource-group $ResourceGroup `
        --allow-shared-key-access false `
        --enable-hierarchical-namespace `
        --subscription $SubscriptionId} | ConvertFrom-Json) 2>&1
    if ($LASTEXITCODE -ne 0 -or !$stg) {
        Write-Host "Error creating storage $storageAccountName - $errOut." `
            -ForegroundColor Red
        exit -1
    }
    Write-Host "Storage account $($stg.id) created." -ForegroundColor Green
}
else {
    Write-Host "Storage account $($stg.id) exists." -ForegroundColor Green
}

# Keyvault
$keyVaultName = $Name + "kv"
$errOut = $($kv = & {az keyvault show `
    --name $keyVaultName `
    --subscription $SubscriptionId} | ConvertFrom-Json) 2>&1
if (!$kv) {
    Write-Host "Creating Key vault $keyVaultName" -ForegroundColor Cyan
    az keyvault purge --name $keyVaultName --location $Location `
        --subscription $SubscriptionId 2>&1 | Out-Null
    $errOut = $($kv = & {az keyvault create `
        --enable-rbac-authorization true `
        --name $keyVaultName `
        --resource-group $ResourceGroup `
        --subscription $SubscriptionId} | ConvertFrom-Json) 2>&1
    if ($LASTEXITCODE -ne 0 -or !$kv) {
        Write-Host "Error creating Azure Keyvault - $errOut." `
            -ForegroundColor Red
        exit -1
    }
    if ($kv.properties.enableSoftDelete) {
        az keyvault update `
            --name $keyVaultName `
            --enable-soft-delete false `
            --resource-group $ResourceGroup `
            --subscription $SubscriptionId 2>&1 | Out-Null
    }
    Write-Host "Key vault $($kv.id) created..." -ForegroundColor Green
}
else {
    Write-Host "Key vault $($kv.id) exists." -ForegroundColor Green
}

# Azure IoT Operations schema registry
$srName = "$($Name.ToLowerInvariant())sr"
$errOut = $($sr = & {az iot ops schema registry show `
    --name $srName `
    --resource-group $ResourceGroup `
    --subscription $SubscriptionId} | ConvertFrom-Json) 2>&1
if (!$sr) {
    Write-Host "Creating Azure IoT Operations schema registry..." -ForegroundColor Cyan
    $errOut = $($sr = & {az iot ops schema registry create `
        --name $srName `
        --resource-group $ResourceGroup `
        --registry-namespace $srName `
        --location $Location `
        --sa-resource-id $stg.id `
        --subscription $SubscriptionId} | ConvertFrom-Json) 2>&1
    if ($LASTEXITCODE -ne 0 -or !$sr) {
        Write-Host "Error creating Azure IoT Operations schema registry - $errOut." `
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
# Assign roles to the managed identity
#
$roleassignments =
@(
    @("Contributor", $stg.id, $mi.principalId),
    @("Storage Blob Data Owner", $stg.id, $mi.principalId),
    @("Key Vault Administrator", $kv.id, $mi.principalId)
)
foreach ($ra in $roleassignments) {
    Write-Host "Assigning $($ra[0]) role to $($ra[2])..." -ForegroundColor Cyan
    $errOut = $($obj = & {az role assignment create `
        --role $ra[0] `
        --assignee-object-id $ra[2] `
        --assignee-principal-type ServicePrincipal `
        --scope $ra[1] | ConvertFrom-Json}) 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Error assigning role $($ra[0]) to $($ra[2]) : $errOut" `
            -ForegroundColor Red
        #exit -1
    }
    Write-Host "Role $($ra[0]) assigned to $($ra[2])." -ForegroundColor Green
}

#
# Connect the cluster to Arc
#
$errOut = $($cc = & {az connectedk8s show `
    --name $Name `
    --resource-group $Name `
    --subscription $SubscriptionId} | ConvertFrom-Json) 2>&1
if (!$cc -or $forceReinstall) {
    Write-Host "Connecting cluster to Arc in $($rg.Name)..." -ForegroundColor Cyan
    $cc = az connectedk8s connect -n $Name -g $Name --subscription $SubscriptionId `
        --correlation-id "d009f5dd-dba8-4ac7-bac9-b54ef3a6671a" 2>&1 | Out-Host
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Error: connecting cluster to Arc failed." -ForegroundColor Red
        exit -1
    }
    Write-Host "Cluster $Name connected to Arc." -ForegroundColor Green
}
else {
    Write-Host "Cluster $($cc.name) already connected." -ForegroundColor Green
}

$errOut = $($iotops = & {az iot ops show `
    --resource-group $ResourceGroup `
    --name $Name `
    --subscription $SubscriptionId} | ConvertFrom-Json) 2>&1
if (!$iotops) {
    Write-Host "Initializing cluster $Name for deployment of Azure IoT operations..." `
        -ForegroundColor Cyan
    az iot ops init `
        --cluster $Name `
        --resource-group $ResourceGroup `
        --ensure-latest `
        --subscription $SubscriptionId `
        --enable-fault-tolerance
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Error initializing cluster $Name for Azure IoT Operations." `
            -ForegroundColor Red
        exit -1
    }
    Write-Host "Cluster ready for Azure IoT Operations deployment..." `
        -ForegroundColor Green
    Write-Host "Creating the Azure IoT Operations instance..." -ForegroundColor Cyan
    az iot ops create `
        --cluster $Name `
        --resource-group $ResourceGroup `
        --name $Name `
        --location $Location `
        --sr-resource-id $sr.id `
        --enable-rsync true `
        --add-insecure-listener true `
        --subscription $SubscriptionId
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Error creating Azure IoT Operations instance - $errOut." `
            -ForegroundColor Red
        exit -1
    }
    Write-Host "Azure IoT Operations instance $Name created." `
        -ForegroundColor Green
}
else {
    Write-Host "Upgrading Azure IoT Operations instance $($iotops.id)..." `
        -ForegroundColor Cyan
    az iot ops update `
        --name $iotops.name  `
        --resource-group $ResourceGroup `
        --subscription $SubscriptionId
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Error upgrading Azure IoT Operations instance - $errOut." `
            -ForegroundColor Red
        exit -1
    }
    Write-Host "Azure IoT Operations $($iotops.id) upgraded..." `
        -ForegroundColor Green
}

$errOut = $($mia = & {az iot ops identity show `
    --name $iotops.name `
    --resource-group $ResourceGroup `
    --subscription $SubscriptionId} | ConvertFrom-Json) 2>&1
if (!$mia) {
    Write-Host "Assign managed identity $($mi.id) to instance $($iotops.id)..." `
        -ForegroundColor Cyan
    $errOut = $($mia = & {az iot ops identity assign `
        --name $iotops.name `
        --resource-group $ResourceGroup `
        --subscription $SubscriptionId `
        --mi-user-assigned $mi.id} | ConvertFrom-Json) 2>&1
    if ($LASTEXITCODE -ne 0 -or !$mia) {
        Write-Host "Error assigning managed identity to instance - $errOut." `
            -ForegroundColor Red
        exit -1
    }
    $mia | Out-Host
    Write-Host "Managed identity $($mi.id) assigned to instance $($iotops.id)." `
        -ForegroundColor Green
}
else {
    Write-Host "Managed identity $($mi.id) already assigned to $($iotops.id)." `
        -ForegroundColor Green
}

$errOut = $($ss = & {az iot ops secretsync show `
    --name $iotops.name `
    --resource-group $ResourceGroup `
    --subscription $SubscriptionId } | ConvertFrom-Json) 2>&1
if (!$ss) {
    Write-Host "Enabling secret sync with $(kv.id) for instance $($iotops.id)..." `
        -ForegroundColor Cyan
    $errOut = $($ss = & {az iot ops secretsync enable `
        --name $iotops.name `
        --kv-resource-id $kv.id `
        --resource-group $ResourceGroup `
        --subscription $SubscriptionId `
        --mi-user-assigned $mi.id} | ConvertFrom-Json) 2>&1
    if ($LASTEXITCODE -ne 0 -or !$ss) {
        Write-Host "Error enabling secret sync for instance - $errOut." `
            -ForegroundColor Red
        exit -1
    }
    Write-Host "Secret sync with $(kv.id) enabled for $($iotops.id)." `
        -ForegroundColor Green
}
else {
    Write-Host "Secret sync with $(kv.id) already enabled in $($iotops.id)." `
        -ForegroundColor Green
}

exit 0

Write-Host "Creating eventhub namespace $Name..." -ForegroundColor Cyan
az eventhubs namespace create --name $Name --resource-group $ResourceGroup `
    --location $Location `
    --disable-local-auth true `
    --subscription $SubscriptionId
Write-Host "Creating eventhub $Name..." -ForegroundColor Cyan
az eventhubs eventhub create --name $Name `
    --resource-group $ResourceGroup `
    --location $Location `
    --namespace-name $Name `
    --retention-time 1 `
    --partition-count 1 `
    --cleanup-policy Delete `
    --enable-capture true `
    --capture-interval 60 `
    --destination-name EventHubArchive.AzureBlockBlob `
    --storage-account $storageAccountName `
    --blob-container "data" `
    --subscription $SubscriptionId

Write-Host "Creating data flow to event hub..." -ForegroundColor Cyan
(Get-Content -Raw dataflow.yml) `
    -replace "<namespace>", "$Name" `
    -replace "<EVENTHUB>", "$Name" | Set-Content -NoNewLine dataflow-$Name.yml
kubectl apply -f dataflow-$Name.yml  --wait
Remove-Item dataflow-$Name.yml

$runtimeNamespace = "azure-iot-operations"
$numberOfPlcs = 10
$numberOfAssets = 90
Write-Host "Creating $numberOfPlcs OPC PLCs..." -ForegroundColor Cyan
helm upgrade -i aio-opc-plc ..\..\distrib\helm\microsoft-opc-plc\ `
    --namespace $runtimeNamespace `
    --set simulations=$numberOfPlcs `
    --set deployDefaultIssuerCA=false `
    --wait

Write-Host "Creating asset endpoint profiles..." -ForegroundColor Cyan
for ($i = 0; $i -lt $numberOfPlcs; $i++) {
    az iot ops asset endpoint create -n aep-$i -g $ResourceGroup `
        -c $Name --target-address "opc.tcp://opcplc-00000$($i%$numberOfPlcs):50000" `
        --additional-config '{\"applicationName\": \"opcua-connector\", \"defaults\": { \"publishingIntervalMilliseconds\": 100,  \"samplingIntervalMilliseconds\": 500,  \"queueSize\": 15,}, \"session\": {\"timeout\": 60000}, \"subscription\": {\"maxItems\": 1000}, \"security\": { \"autoAcceptUntrustedServerCertificates\": true}}'
}

Write-Host "Creating assets..." -ForegroundColor Cyan
# https://learn.microsoft.com/en-us/cli/azure/iot/ops/asset/data-point?view=azure-cli-latest#az-iot-ops-asset-data-point-add
for ($i = 0; $i -lt $numberOfAssets; $i++) {
    Write-Host "Creating asset asset-$i"
    az iot ops asset create -n asset-$i -g $ResourceGroup `
        --endpoint aep-$($i%$numberOfPlcs) -c $Name --data capability_id=FastUInt1 data_source="nsu=http://microsoft.com/Opc/OpcPlc/;s=FastUInt1" 
    Write-Host "Importing data points..."
    az iot ops asset data-point import --asset asset-$i `
        --resource-group $ResourceGroup --input-file .\asset_dataPoints.json
}

Write-Host "Creating asset asset-with-events" -ForegroundColor Cyan
az iot ops asset create -n asset-with-events -g $ResourceGroup `
    --endpoint aep-0 -c $Name --event capability_id=Event1 event_notifier="ns=0; i=2253"

helm repo add jetstack https://charts.jetstack.io --force-update
helm repo update
helm upgrade cert-manager jetstack/cert-manager --install `
    --namespace cert-manager `
    --create-namespace `
    --set crds.enabled=true `
    --wait
helm repo add akri-helm-charts https://project-akri.github.io/akri/
helm repo update
helm upgrade --install akri akri-helm-charts/akri `
    --namespace akri `
    --create-namespace `
    --wait
helm upgrade --install aio-mq oci://mcr.microsoft.com/azureiotoperations/helm/aio-broker `
    --version $mqVersion `
    --namespace $mqNamespace `
    --create-namespace `
    --wait

kubectl apply -f ..\..\distrib\helm\mq\broker-sat.yaml -n $mqNamespace
$e4kMqttAddress = 'mqtt://aio-broker.' + $mqNamespace + ':1883'
helm repo add prometheus-community https://prometheus-community.github.io/helm-charts/
helm repo update
