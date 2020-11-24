Param(
    [string]
    $ResourceGroupName,
    [int]
    $NumberOfSimulations = 18,
    [Guid]
    $TenantId
)

$ErrorActionPreference = "Stop"

if (!$ResourceGroupName) {
    Write-Error "ResourceGroupName not set."
}

$templateDir = [System.IO.Path]::Combine($PSScriptRoot, "../../deploy/templates") 
$edgeVmUsername = "sandboxuser"
$edgeVmPassword = -join ((65..90) + (97..122) + (33..38) + (40..47) + (48..57)| Get-Random -Count 20 | % {[char]$_})

$context = Get-AzContext

if (!$context) {
    Write-Host "Logging in..."
    Login-AzAccount -Tenant $TenantId
    $context = Get-AzContext
}

$resourceGroup = Get-AzResourceGroup -Name $resourceGroupName

if (!$resourceGroup) {
    Write-Error "Could not find Resource Group '$($ResourceGroupName)'."
}

$testSuffix = $resourceGroup.Tags["TestingResourcesSuffix"]

if (!$testSuffix) {
    $testSuffix = Get-Random -Minimum 10000 -Maximum 99999

    $tags = $resourceGroup.Tags
    $tags+= @{"TestingResourcesSuffix" = $testSuffix}
    Set-AzResourceGroup -Name $resourceGroup.ResourceGroupName -Tag $tags | Out-Null
    $resourceGroup = Get-AzResourceGroup -Name $resourceGroup.ResourceGroupName
}

Write-Host "Using suffix for testing resources: $($testSuffix)"

# Get IoT Hub
$iotHub = Get-AzIotHub -ResourceGroupName $ResourceGroupName

if ($iotHub.Count -ne 1) {
    Write-Error "IotHub could not be automatically selected in Resource Group '$($ResourceGroupName)'."    
}

Write-Host "IoT Hub Name: $($iotHub.Name)"

$keyVault = Get-AzKeyVault -ResourceGroupName $ResourceGroupName

if ($keyVault.Count -ne 1) {
    Write-Error "keyVault could not be automatically selected in Resource Group '$($ResourceGroupName)'."    
} 

Write-Host "Key Vault Name: $($keyVault.VaultName)"

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
Set-AzKeyVaultSecret -VaultName $keyVault.VaultName -Name 'iot-edge-device-id' -SecretValue (ConvertTo-SecureString $edgeIdentity.Id -AsPlainText -Force) | Out-Null

Write-Host "Updating 'os' and '__type__'-Tags in Device Twin..."
Update-AzIotHubDeviceTwin -ResourceGroupName $ResourceGroupName -IotHubName $iotHub.Name -DeviceId $edgeIdentity.Id -Tag @{ "os" = "Linux"; "__type__" = "iiotedge"} | Out-Null

# Deploy edge and simulation virtual machines
$plcTemplateParameters = @{
    "factoryName" = "contoso"
    "numberOfSimulations" = $numberOfSimulations
    "numberOfSlowNodes" = 1000
    "slowNodeRate" = 10
    "slowNodeType" = "uint"
    "numberOfFastNodes" = 1000
    "fastNodeRate" = 1
    "fastNodeType" = "uint"
}

Write-Host "Preparing to deploy e2e.plc.simulation.json"

$plcTemplate = [System.IO.Path]::Combine($TemplateDir, "e2e.plc.simulation.json")
$plcDeployment = New-AzResourceGroupDeployment -ResourceGroupName $ResourceGroupName -TemplateFile $plcTemplate -TemplateParameterObject $plcTemplateParameters

if ($plcDeployment.ProvisioningState -ne "Succeeded") {
    Write-Error "Deployment $($plcDeployment.ProvisioningState)." -ErrorAction Stop
}

Write-Host "Getting IPs of ACIs for simulated PLCs..."

$containerInstances = Get-AzContainerGroup -ResourceGroupName $ResourceGroupName

$plcSimNames = ""
foreach ($ci in $containerInstances) {
    $plcSimNames += $ci.Fqdn + ";"
}

Write-Host "Adding/Updating KeVault-Secret 'plc-simulation-urls' with value '$($plcSimNames)'..."
Set-AzKeyVaultSecret -VaultName $keyVault.VaultName -Name 'plc-simulation-urls' -SecretValue (ConvertTo-SecureString $plcSimNames -AsPlainText -Force) | Out-Null

Write-Host "Getting Device Connection String for IoT Edge Deployment..."
$edgeDeviceConnectionString = Get-AzIotHubDeviceConnectionString -ResourceGroupName $ResourceGroupName -IotHubName $iotHub.Name -DeviceId $edgeIdentity.Id -KeyType primary
$edgeDeviceConnectionString = $edgeDeviceConnectionString.ConnectionString

$dnsPrefix = "e2etesting-edgevm-" + $testSuffix

Write-Host "Using DNS prefix: $($dnsPrefix)"

$edgeParameters = @{
    "dnsLabelPrefix" = $dnsPrefix
    "adminUsername" = $edgeVmUsername
    "deviceConnectionString" = $edgeDeviceConnectionString
    "authenticationType" = "password"
    "adminPasswordOrKey" = $edgeVmPassword
}

$edgeTemplateUri = "https://aka.ms/iotedge-vm-deploy"

Write-Host "Running IoT Edge VM Deployment..."

$edgeDeployment = New-AzResourceGroupDeployment -ResourceGroupName $ResourceGroupName -TemplateUri $edgeTemplateUri -TemplateParameterObject $edgeParameters

if ($edgeDeployment.ProvisioningState -ne "Succeeded") {
    Write-Error "Deployment $($edgeDeployment.ProvisioningState)." -ErrorAction Stop
}

Write-Host "Adding/Updating KeVault-Secret 'pcs-simulation-user' with value '$($edgeVmUsername)'..."
Write-Host "Adding/Updating KeVault-Secret 'pcs-simulation-password' with value '***'..."

Set-AzKeyVaultSecret -VaultName $keyVault.VaultName -Name 'pcs-simulation-user' -SecretValue (ConvertTo-SecureString $edgeVmUsername -AsPlainText -Force) | Out-Null
Set-AzKeyVaultSecret -VaultName $keyVault.VaultName -Name 'pcs-simulation-password' -SecretValue (ConvertTo-SecureString $edgeVmPassword -AsPlainText -Force) | Out-Null

$sshUrl = $edgeDeployment.Outputs["public SSH"].Value
$fqdn = $sshUrl.Split("@")[1]

Write-Host "Adding/Updating KeVault-Secret 'iot-edge-device-dns-name' with value '$($fqdn)'..."
Set-AzKeyVaultSecret -VaultName $keyVault.VaultName -Name 'iot-edge-device-dns-name' -SecretValue (ConvertTo-SecureString $fqdn -AsPlainText -Force) | Out-Null

Write-Host "Deployment finished."

