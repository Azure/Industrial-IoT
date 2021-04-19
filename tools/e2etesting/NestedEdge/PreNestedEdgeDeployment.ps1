Param(
    [string]
    $ResourceGroupName,
    [string]
    $KeysPath,
    [Guid]
    $TenantId
)

# Stop execution when an error occurs.
$ErrorActionPreference = "Stop"

if (!$ResourceGroupName) {
    Write-Error "ResourceGroupName not set."
}

## Login if required

$context = Get-AzContext

if (!$context) {
    Write-Host "Logging in..."
    Login-AzAccount -Tenant $TenantId
    $context = Get-AzContext
}

## Check if resource group exists

$resourceGroup = Get-AzResourceGroup -Name $resourceGroupName

if (!$resourceGroup) {
    Write-Error "Could not find Resource Group '$($ResourceGroupName)'."
}

## Determine suffix for testing resources

$testSuffix = $resourceGroup.Tags["TestingResourcesSuffix"]

if (!$testSuffix) {
    $testSuffix = Get-Random -Minimum 10000 -Maximum 99999

    $tags = $resourceGroup.Tags
    $tags+= @{"TestingResourcesSuffix" = $testSuffix}
    Set-AzResourceGroup -Name $resourceGroup.ResourceGroupName -Tag $tags | Out-Null
    $resourceGroup = Get-AzResourceGroup -Name $resourceGroup.ResourceGroupName
}

Write-Host "Using suffix for testing resources: $($testSuffix)"

## Check if IoT Hub exists
$iotHub = Get-AzIotHub -ResourceGroupName $ResourceGroupName

if ($iotHub.Count -ne 1) {
    Write-Error "IotHub could not be automatically selected in Resource Group '$($ResourceGroupName)'."    
}

Write-Host "IoT Hub Name: $($iotHub.Name)"
Write-Host "##vso[task.setvariable variable=iothub]$($iotHub.Name)"

## Check if KeyVault exists
$keyVaultList = Get-AzKeyVault -ResourceGroupName $ResourceGroupName

if ($keyVaultList.Count -ne 1) {
    Write-Host "keyVault could not be automatically selected in Resource Group '$($ResourceGroupName)'."    
    # There is a bug for Get-AzKeyVault in the new Powershell version
    $keyVault = "e2etestingkeyVault" + $resourceGroup.Tags["TestingResourcesSuffix"]
} 
else {
    $keyVault = $keyVaultList.VaultName
}

Write-Host "Key Vault Name: $($keyVault)"

## Create ACR
$acrName = "ACR" + $testSuffix
$registry = New-AzContainerRegistry -ResourceGroupName $ResourceGroupName -Name $acrName -EnableAdminUser -Sku Basic
$creds = Get-AzContainerRegistryCredential -Registry $registry

## Update ACR.env file
$fileName = "ACR.env"
$currentPath = (Get-Location).Path
$path = Get-ChildItem -Path $currentPath $fileName -Recurse -ErrorAction SilentlyContinue
Write-Host "The solution path is: $path"

$acrFile = "./tools/e2etesting/NestedEdge/ACR.env"
$arcEnvOriginal =  Get-Content $acrFile -Raw 
$acrEnv = $arcEnvOriginal
$acrEnv = $acrEnv -replace 'YOUR_ACR_ADDRESS', ($creds.Username + ".azurecr.io")
$acrEnv = $acrEnv -replace 'YOUR_ACR_USERNAME', $creds.Username
$acrEnv = $acrEnv -replace 'YOUR_ACR_PASSWORD', $creds.Password
$acrEnv | Out-File $acrFile

## Generate SSH keys
Write-Host "Keys path: $KeysPath"

$privateKeyFilePath = Join-Path $KeysPath "id_rsa"
$publicKeyFilePath = $privateKeyFilePath + ".pub"
$keypassphrase = '""'
Write-Output "y" | ssh-keygen -q -m PEM -b 4096 -t rsa -f $privateKeyFilePath -N $keypassphrase
$sshPrivateKey = Get-Content $privateKeyFilePath -Raw 
$sshPublicKey = Get-Content $publicKeyFilePath -Raw

# Store ssh keys
Write-Host "Adding/Updating KeVault-Certificate 'iot-edge-vm-privatekey'..."
Set-AzKeyVaultSecret -VaultName $keyVault -Name 'iot-edge-vm-privatekey' -SecretValue (ConvertTo-SecureString $sshPrivateKey -AsPlainText -Force) | Out-Null

Write-Host "Adding/Updating KeVault-Certificate 'iot-edge-vm-publickey'..."
Set-AzKeyVaultSecret -VaultName $keyVault -Name 'iot-edge-vm-publickey' -SecretValue (ConvertTo-SecureString $sshPublicKey -AsPlainText -Force) | Out-Null