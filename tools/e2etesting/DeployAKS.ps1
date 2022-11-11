Param(
    [string]
    $ResourceGroupName,
    [Guid]
    $TenantId,
    [String]
    $Region = "northeurope",
    [string] 
    $PublisherDeploymentFile = "./K8s-Standalone/publisher/deployment.yaml",
    [string] 
    $ContainerRegistryServer = "mcr.microsoft.com",
    [string] 
    $ContainerRegistryUsername,
    [string]
    $ContainerRegistryPassword,
    [string]
    $ImageNamespace = "",
    [string]
    $ImageTag = "latest"
)

# Stop execution when an error occurs.
$ErrorActionPreference = "Stop"

if (!$ResourceGroupName) {
    Write-Error "ResourceGroupName not set."
}

if (!$Region) {
    Write-Error "Region not set."
}

if (!(Microsoft.PowerShell.Management\Test-Path -Path $PublisherDeploymentFile -PathType Leaf)) {
    Write-Error "OPC Publisher k8s deployment file '$PublisherDeploymentFile' does not exist"
}

## show installed az.aks module
Get-Module -listAvailable -Name Az.Aks, Az.ContainerRegistry

## Login if required

$context = Get-AzContext

if (!$context) {
    Write-Host "Logging in..."
    Login-AzAccount -Tenant $TenantId
    $context = Get-AzContext
}

## Check if resource group exists

$resourceGroup = Get-AzResourceGroup -Name $resourceGroupName -ErrorAction SilentlyContinue

if (!$resourceGroup) {
    Write-Host "Creating Resource Group $($ResourceGroupName) in $($Region)..."
    $resourceGroup = New-AzResourceGroup -Name $ResourceGroupName -Location $Region
}
else {
    Write-Host "Using resource Group: $($resourceGroup.ResourceGroupName)"
}

## Build verifier
$registryName = "$($ResourceGroupName)acr"

$registry = Get-AzContainerRegistry -ResourceGroupName $ResourceGroupName -Name $registryName -ErrorAction SilentlyContinue
if (!$registry) {
    Write-Host "Creating container registry $($registryName) in $($Region) ..."
    $registry = New-AzContainerRegistry -ResourceGroupName $ResourceGroupName -Name $registryName -EnableAdminUser -Sku Standard -Location $Region
}
else {
    Write-Host "Using conainer registry: $($registry.Name)"
}

$registrySecret = Get-AzContainerRegistryCredential -ResourceGroupName $ResourceGroupName -Name $registryName

Connect-AzContainerRegistry -Name $registryName
$verifierImageName = "$($registry.LoginServer)/mqtt-verifier:latest"
Write-Host "Build and push verifier image $($verifierImageName) created."
docker build -t mqtt-verifier -f ./tools/e2etesting/MqttTestValidator/MqttTestValidator/Dockerfile ./tools/e2etesting/MqttTestValidator/MqttTestValidator
docker image tag mqtt-verifier $verifierImageName
docker push $verifierImageName
Write-Host "Verifier image $($verifierImageName) created."

## Determine suffix for testing resources
if (!$resourceGroup.Tags) {
    $resourceGroup.Tags = @{}
}

$testSuffix = $resourceGroup.Tags["TestingResourcesSuffix"]

if (!$testSuffix) {
    $testSuffix = Get-Random -Minimum 10000 -Maximum 99999

    $tags = $resourceGroup.Tags
    $tags += @{"TestingResourcesSuffix" = $testSuffix }
    Set-AzResourceGroup -Name $resourceGroup.ResourceGroupName -Tag $tags | Out-Null
    $resourceGroup = Get-AzResourceGroup -Name $resourceGroup.ResourceGroupName


    $aksName = "aksCluster_$($testSuffix)"

    # Create ssh keys
    Write-Host "Creating ssh key"
    ssh-keygen -m PEM -t rsa -b 4096 -f ssh -q -N '""'

    Get-Content ssh.pub

    ## Create AKS Cluster
    Write-Host "Creating cluster $aksName"

    $aksCluster = New-AzAksCluster -ResourceGroupName $resourceGroupName -Name $aksName -NodeCount 3 -SshKeyPath ssh.pub -Force 

    if (!$aksCluster) {
        Write-Error "Failed to create AKS cluster."
    }
    else {
        Write-Host "Cluster $aksName created"
        $aksCluster | Format-Table | Out-String | % { Write-Host $_ }
    }

}else{
    $aksName = "aksCluster_$($testSuffix)"
}

## Install kubectl
Install-AzAksKubectl -Version latest -Force


## Load AKS Cluster credentials
Import-AzAksCredential -ResourceGroupName $resourceGroupName -Name $aksName -Force

## Create testing namespace in AKS
kubectl apply -f ./tools/e2etesting/K8s-Standalone/e2etesting/

## Load Mosquitto
kubectl apply -f ./tools/e2etesting/K8s-Standalone/mosquitto/

## Load OPC PLC
kubectl apply -f ./tools/e2etesting/K8s-Standalone/opcplc/

## Load OPC Publisher

$deviceId = "device_$($testSuffix)"

### Create Image Pull Secret if required
if (![string]::IsNullOrEmpty($ContainerRegistryUsername) && $ContainerRegistryPassword.Length -ne 0) {
    $withImagePullSecret = $true
    # $temp = ConvertFrom-SecureString -SecureString $ContainerRegistryPassword -AsPlainText
    kubectl create secret docker-registry dev-registry-pull-secret --docker-server=$ContainerRegistryServer --docker-username=$ContainerRegistryUsername --namespace=e2etesting --docker-password=$ContainerRegistryPassword
    $temp = $null
}
else {
    $withImagePullSecret = $false
}

### Replace placeholder in deployment file
$fileContent = Get-Content $PublisherDeploymentFile -Raw
$fileContent = $fileContent -replace "{{ContainerRegistryServer}}", $ContainerRegistryServer
$fileContent = $fileContent -replace "{{ImageNamespace}}", $ImageNamespace
$fileContent = $fileContent -replace "{{ImageTag}}", $ImageTag
$fileContent = $fileContent -replace "{{DeviceId}}", $deviceId
if ($withImagePullSecret) {
    $fileContent = $fileContent -replace "{{ImagePullSecret}}", ""
}
else {
    $fileContent = $fileContent -replace "{{ImagePullSecret}}", "#"
}
$fileContent | Out-File $PublisherDeploymentFile -Force -Encoding utf8

kubectl apply -f ./tools/e2etesting/K8s-Standalone/publisher

$fileContent = Get-Content './tools/e2etesting/K8s-Standalone/verifier/deployment.yaml' -Raw
$fileContent = $fileContent -replace "{{VerifierImage}}", $verifierImageName
$fileContent = $fileContent -replace "{{ImagePullSecret}}", $registrySecret.Password
$fileContent | Out-File './tools/e2etesting/K8s-Standalone/verifier/deployment.yaml' -Force -Encoding utf8

kubectl apply -f ./tools/e2etesting/K8s-Standalone/verifier