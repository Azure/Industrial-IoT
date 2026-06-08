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
    Write-Host "Creating container registry $($registryName) in $($Region) (admin user disabled; AAD-only)..."
    $registry = New-AzContainerRegistry -ResourceGroupName $ResourceGroupName -Name $registryName -Sku Standard -Location $Region
}
else {
    Write-Host "Using conainer registry: $($registry.Name)"
    # Ensure admin user is disabled on existing registries; AAD-only.
    if ($registry.AdminUserEnabled) {
        Write-Host "Disabling admin user on $($registryName) for AAD-only authentication."
        Update-AzContainerRegistry -ResourceGroupName $ResourceGroupName -Name $registryName -DisableAdminUser
    }
}

# AAD login to the registry using the current az context (federated SP).
# No admin credentials are read or stored anywhere.
Connect-AzContainerRegistry -Name $registryName
$verifierImageName = "$($registry.LoginServer)/mqtt-verifier:latest"
Write-Host "Build and push verifier image $($verifierImageName)..."
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

    $aksName = "aksCluster_$($testSuffix)"

    # Create ssh keys
    Write-Host "Creating ssh key"
    ssh-keygen -m PEM -t rsa -b 4096 -f ssh -q -N '""'

    Get-Content ssh.pub

    ## Create AKS Cluster
    Write-Host "Creating cluster $aksName"

    for ($i = 0; ($i -lt 20) -and (!$aksCluster); $i++) {
        try {
            $aksCluster = New-AzAksCluster -ResourceGroupName $resourceGroupName -Name $aksName -NodeCount 3 -SshKeyPath ssh.pub -Force
            if (!$aksCluster) {
                throw "Failed to create AKS cluster."
            }
            else {
                Write-Host "Cluster $aksName created"
                $aksCluster | Format-Table | Out-String | % { Write-Host $_ }
            }
        }
        catch {
            Write-Host "$($_.Exception.Message) for $($aksName) - Retrying..."
            Start-Sleep -s 2
        }
    }

    if (!$aksCluster) {
        Write-Error "Failed to create AKS cluster."
    }
    else {
        $tags = $resourceGroup.Tags
        $tags += @{"TestingResourcesSuffix" = $testSuffix }
        Set-AzResourceGroup -Name $resourceGroup.ResourceGroupName -Tag $tags | Out-Null
        $resourceGroup = Get-AzResourceGroup -Name $resourceGroup.ResourceGroupName
    }
} else {
    $aksName = "aksCluster_$($testSuffix)"
}

## Install kubectl
Install-AzAksKubectl -Version latest -Force

## Load AKS Cluster credentials
Import-AzAksCredential -ResourceGroupName $resourceGroupName -Name $aksName -Force

## Grant AKS kubelet managed identity AcrPull on the test ACR so the cluster can pull
## the verifier image (and any other private images we publish to this ACR) without
## the docker-registry image-pull secret. This replaces the previous secret-based flow.
$aksCluster = Get-AzAksCluster -ResourceGroupName $resourceGroupName -Name $aksName
$kubeletObjectId = $aksCluster.IdentityProfile.kubeletidentity.ObjectId
if ($kubeletObjectId) {
    $existing = Get-AzRoleAssignment -ObjectId $kubeletObjectId -RoleDefinitionName "AcrPull" -Scope $registry.Id -ErrorAction SilentlyContinue
    if (!$existing) {
        Write-Host "Granting AcrPull on $($registryName) to AKS kubelet identity $kubeletObjectId..."
        New-AzRoleAssignment -ObjectId $kubeletObjectId -RoleDefinitionName "AcrPull" -Scope $registry.Id | Out-Null
    }
} else {
    Write-Warning "AKS kubelet identity not found; cannot grant AcrPull. Verifier image pull may fail."
}

## Create testing namespace in AKS
kubectl apply -f ./tools/e2etesting/K8s-Standalone/e2etesting/

## Load Mosquitto
kubectl apply -f ./tools/e2etesting/K8s-Standalone/mosquitto/

## Load OPC PLC
kubectl apply -f ./tools/e2etesting/K8s-Standalone/opcplc/

## Load OPC Publisher

$deviceId = "device_$($testSuffix)"

### Create Image Pull Secret if required (external registry only; the test ACR uses
### the AKS kubelet identity + AcrPull role granted above).
if (![string]::IsNullOrEmpty($ContainerRegistryUsername) -and ($ContainerRegistryPassword.Length -ne 0)) {
    $withImagePullSecret = $true
    kubectl create secret docker-registry dev-registry-pull-secret --docker-server=$ContainerRegistryServer --docker-username=$ContainerRegistryUsername --namespace=e2etesting --docker-password=$ContainerRegistryPassword
}
else {
    $withImagePullSecret = $false
}

### Replace placeholder in deployment file
$fileContent = Get-Content $PublisherDeploymentFile -Raw
$fileContent = $fileContent -replace "{{ContainerRegistryServer}}", $ContainerRegistryServer
if (![string]::IsNullOrEmpty($ImageNamespace)) {
    $ImageNamespace = "$($ImageNamespace)/"
}
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
$fileContent | Out-Host

kubectl apply -f ./tools/e2etesting/K8s-Standalone/publisher

$fileContent = Get-Content './tools/e2etesting/K8s-Standalone/verifier/deployment.yaml' -Raw
$fileContent = $fileContent -replace "{{VerifierImage}}", $verifierImageName
$fileContent | Out-File './tools/e2etesting/K8s-Standalone/verifier/deployment.yaml' -Force -Encoding utf8
$fileContent | Out-Host

# No docker-registry secret created for the verifier image — AcrPull on the kubelet
# identity (granted above) handles pulls from the test ACR without stored credentials.
kubectl apply -f ./tools/e2etesting/K8s-Standalone/verifier
