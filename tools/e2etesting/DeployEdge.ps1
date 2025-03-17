Param(
    [string]
    $ResourceGroupName,
    [Guid]
    $TenantId,
    [string]
    $EdgeVmSize = "Standard_D2s_v3",
    [string]
    $EdgeVmLocation,
    [string]
    $KeysPath
)

# Stop execution when an error occurs.
$ErrorActionPreference = "Stop"

if (!$ResourceGroupName) {
    Write-Error "ResourceGroupName not set."
}

if (!$KeysPath) {
    Write-Error "Path to store certifactes not set."
}

if (!(Test-Path -Path $KeysPath)) {
    New-Item -ItemType Directory -Path $KeysPath | Out-Null
}

$edgeVmUsername = 'sandboxuser'

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

## Check if KeyVault exists
$keyVault = Get-AzKeyVault -ResourceGroupName $ResourceGroupName

if ($keyVault.Count -ne 1) {
    Write-Error "keyVault could not be automatically selected in Resource Group '$($ResourceGroupName)'."    
} 

Write-Host "Key Vault Name: $($keyVault.VaultName)"

## Ensure that Edge Device exists

$deviceName = "e2etestdevice_$($testSuffix)"

Write-Host "Iot Hub Device Identity: $($deviceName)"

$edgeIdentity = Get-AzIotHubDevice -ResourceGroupName $ResourceGroupName -IotHubName $iotHub.Name -DeviceId $deviceName -ErrorAction SilentlyContinue

if (!$edgeIdentity) {
    Write-Host "Creating edge-enabled device identity $($deviceName) in Iot Hub $($iotHub.Name)"
    $edgeIdentity = Add-AzIotHubDevice -ResourceGroupName $ResourceGroupName -IotHubName $iotHub.Name -DeviceId $deviceName -EdgeEnabled
}

if (!$edgeIdentity.Capabilities.IotEdge) {
    Write-Error "Device '$($edgeIdentity.Id)' Iot Hub: '$($iotHub.Name)') is not edge-enabled."
}

Write-Host "Adding/Updating KeyVault-Secret 'iot-edge-device-id' with value '$($edgeIdentity.Id)'..."
[Diagnostics.CodeAnalysis.SuppressMessageAttribute("PSAvoidUsingConvertToSecureStringWithPlainText", "")]
$secret = ConvertTo-SecureString $edgeIdentity.Id -AsPlainText -Force
Set-AzKeyVaultSecret -VaultName $keyVault.VaultName -Name 'iot-edge-device-id' -SecretValue $secret | Out-Null

Write-Host "Updating 'os' and '__type__'-Tags in Device Twin..."
Update-AzIotHubDeviceTwin -ResourceGroupName $ResourceGroupName -IotHubName $iotHub.Name -DeviceId $edgeIdentity.Id -Tag @{ "os" = "Linux"; "__type__" = "iiotedge"; } | Out-Null

## Generate SSH keys
$privateKeyFilePath = Join-Path $KeysPath "id_rsa_iotedge"
$publicKeyFilePath = $privateKeyFilePath + ".pub"
$keypassphrase = '"$($testSuffix)"'
Write-Output "y" | ssh-keygen -q -m PEM -b 4096 -t rsa -f $privateKeyFilePath -N $keypassphrase
$sshPrivateKey = Get-Content $privateKeyFilePath -Raw
$sshPublicKey = Get-Content $publicKeyFilePath -Raw

## Delete SSH keys from file system
Remove-Item -Path $privateKeyFilePath | Out-Null
Remove-Item -Path $publicKeyFilePath | Out-Null

## Deploy Edge VM
Write-Host "Getting Device Connection String for IoT Edge Deployment..."
$edgeDeviceConnectionString = Get-AzIotHubDeviceConnectionString -ResourceGroupName $ResourceGroupName -IotHubName $iotHub.Name -DeviceId $edgeIdentity.Id -KeyType primary
$edgeDeviceConnectionString = $edgeDeviceConnectionString.ConnectionString

$dnsPrefix = "e2etesting-edgevm-" + $testSuffix

Write-Host "Using DNS prefix: $($dnsPrefix)"

$edgeParameters = @{
    "dnsLabelPrefix" = [string]$dnsPrefix
    "adminUsername" = [string]$edgeVmUsername
    "deviceConnectionString" = [string]$edgeDeviceConnectionString
    "authenticationType" = "sshPublicKey"
    "adminPasswordOrKey" = [string]$sshPublicKey
    "allowSsh" = $true
    "vmSize" = [string]$EdgeVmSize
}

if ($EdgeVmLocation) {
    $edgeParameters["location"] = [string]$EdgeVmLocation
}

$edgeTemplateUri = "https://raw.githubusercontent.com/Azure/iotedge-vm-deploy/1.4/edgeDeploy.json"

Write-Host "Running IoT Edge VM Deployment..."

$edgeDeployment = New-AzResourceGroupDeployment -ResourceGroupName $ResourceGroupName -TemplateUri $edgeTemplateUri -TemplateParameterObject $edgeParameters

$edgeDeployment | ConvertTo-Json | Out-Host

if ($edgeDeployment.ProvisioningState -ne "Succeeded") {
    Write-Error "Deployment $($edgeDeployment.ProvisioningState)."
}

## This needs to be refactored. However, currently the SSH-Command is the only output from the Edge deployment script. And that command includes the FQDN of the VM.
$sshUrl = $edgeDeployment.Outputs["public_SSH"].Value
if ([string]::IsNullOrEmpty($sshUrl)) {
    Write-Error "Deployment did not provide Public_SSH output."
}
$fqdn = $sshUrl.Split("@")[1]

Write-Host "Adding/Updating KeVault-Secret 'iot-edge-vm-username' with value '$($edgeVmUsername)'..."
[Diagnostics.CodeAnalysis.SuppressMessageAttribute("PSAvoidUsingConvertToSecureStringWithPlainText", "")]
$secret = ConvertTo-SecureString $edgeVmUsername -AsPlainText -Force
Set-AzKeyVaultSecret -VaultName $keyVault.VaultName -Name 'iot-edge-vm-username' -SecretValue $secret | Out-Null

Write-Host "Adding/Updating KeVault-Certificate 'iot-edge-vm-privatekey'..."
[Diagnostics.CodeAnalysis.SuppressMessageAttribute("PSAvoidUsingConvertToSecureStringWithPlainText", "")]
$secret = ConvertTo-SecureString $sshPrivateKey -AsPlainText -Force
Set-AzKeyVaultSecret -VaultName $keyVault.VaultName -Name 'iot-edge-vm-privatekey' -SecretValue $secret | Out-Null

Write-Host "Adding/Updating KeVault-Certificate 'iot-edge-vm-publickey'..."
[Diagnostics.CodeAnalysis.SuppressMessageAttribute("PSAvoidUsingConvertToSecureStringWithPlainText", "")]
$secret = ConvertTo-SecureString $sshPublicKey -AsPlainText -Force
Set-AzKeyVaultSecret -VaultName $keyVault.VaultName -Name 'iot-edge-vm-publickey' -SecretValue $secret | Out-Null

Write-Host "Adding/Updating KeyVault-Secret 'iot-edge-device-dnsname' with value '$($fqdn)'..."
[Diagnostics.CodeAnalysis.SuppressMessageAttribute("PSAvoidUsingConvertToSecureStringWithPlainText", "")]
$secret = ConvertTo-SecureString $fqdn -AsPlainText -Force
Set-AzKeyVaultSecret -VaultName $keyVault.VaultName -Name 'iot-edge-device-dnsname' -SecretValue $secret | Out-Null

Write-Host "Deployment finished."