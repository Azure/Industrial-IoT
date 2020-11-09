Param(
    [string]
    $keyVaultName,
    [string]
    $resourceGroupName,
    [string]
    $branchName
)

$branchName = $branchName.Replace('refs/heads/', '')
$templateDir = [System.IO.Path]::Combine($PSScriptRoot, "../../deploy/templates") 

# Get IoT Hub
$iotHub = Get-AzIotHub -ResourceGroupName $resourceGroupName
$ioTHubConnString = (Get-AzIotHubConnectionString -ResourceGroupName $resourceGroupName -KeyName iothubowner -Name $iotHub.Name).PrimaryConnectionString

# Create DPS
# Write-Host "Creating device provisioning service"

# $templateParameters = @{
#     "dpsIotHubHostName" = $iotHub.Properties.HostName
#     "dpsIotHubConnectionString" = $ioTHubConnString
#     "dpsIotHubLocation" = $iotHub.Location
#     "keyVaultName" = $keyVaultName
#     "branchName" = $branchName
# }

# $template = [System.IO.Path]::Combine($templateDir, "azuredeploy.deviceprovisioning.json")
# $dpsDeployment = New-AzResourceGroupDeployment -ResourceGroupName $resourceGroupName -TemplateFile $template -TemplateParameterObject $templateParameters
# if ($dpsDeployment.ProvisioningState -ne "Succeeded") {
#     Write-Error "Deployment $($dpsDeployment.ProvisioningState)." -ErrorAction Stop
# }

# Write-Host "Created DPS"

# Create MSI for edge
Write-Host "Creating MSI for edge VM identity"

$template = [System.IO.Path]::Combine($templateDir, "azuredeploy.managedidentity.json")
$msiDeployment = New-AzResourceGroupDeployment -ResourceGroupName $resourceGroupName -TemplateFile $template
if ($msiDeployment.ProvisioningState -ne "Succeeded") {
    Write-Error "Deployment $($msiDeployment.ProvisioningState)." -ErrorAction Stop
}

Write-Host "Created MSI $($msiDeployment.Parameters.managedIdentityName.Value) with resource id $($msiDeployment.Outputs.managedIdentityResourceId.Value)"

# Configure the keyvault
# Allow the MSI to access keyvault
# https://github.com/Azure/azure-powershell/issues/10029 for why -BypassObjectIdValidation is needed
Set-AzKeyVaultAccessPolicy -VaultName $keyVaultName -ObjectId $msiDeployment.Outputs.managedIdentityPrincipalId.Value -PermissionsToSecrets get,list -BypassObjectIdValidation
Write-Host "Key vault set to allow MSI full access"

# Allow the keyvault to be used in ARM deployments
Set-AzKeyVaultAccessPolicy -VaultName $keyVaultName -EnabledForTemplateDeployment
Write-Host "Key vault configured to be used in ARM deployments"

# Register IoT Edge device at IoT Hub
$testEdgeDevice = "myTestDevice_$(Get-Random)"
$edgeIdentity = az iot hub device-identity create --device-id $testEdgeDevice --edge-enabled --hub-name $iotHub.Name | ConvertFrom-Json
if ($edgeIdentity.status -ne "enabled") {
    Write-Error "Deployment edge identify for test device" -ErrorAction Stop
}

# Read Device Connection String for TestDevice
$edgeDeviceConnectionString = az iot hub device-identity connection-string show --device-id $edgeIdentity.deviceId --hub-name $iotHub.Name | ConvertFrom-Json

# Update Device Twin of IoT Edge device
$deviceTwin = az iot hub device-twin show --device-id $edgeIdentity.deviceId --hub-name $iotHub.Name | ConvertFrom-Json
$tags = "" | Select os,__type__
$tags.os = "Linux"
$tags.__type__ = "iiotedge"
$deviceTwin | Add-Member -MemberType NoteProperty -Value $tags -Name "tags"
$configFileName = New-TemporaryFile
($deviceTwin | ConvertTo-Json -Depth 10) | Out-File -FilePath $configFileName -Encoding UTF8
az iot hub device-twin replace --device-id $edgeIdentity.deviceId --hub-name $iotHub.Name --json $configFileName

# Deploy edge and simulation virtual machines
$templateParameters = @{
    "factoryName" = "contoso"
    "vmUsername" = "sandboxuser"
    "vmPassword" = [System.Web.Security.Membership]::GeneratePassword(15, 5)
    "edgeVmSize" = "Standard_D2s_v3"
    "numberOfSimulations" = 18
    "numberOfSlowNodes" = 1000
    "slowNodeRate" = 10
    "slowNodeType" = "uint"
    "numberOfFastNodes" = 1000
    "fastNodeRate" = 1
    "fastNodeType" = "uint"
    "iotEdgeConnString" = $edgeDeviceConnectionString.connectionString
    "keyVaultName" = $keyVaultName
}

Write-Host "Preparing to deploy e2e.edge.and.plc.simulation.json"

$template = [System.IO.Path]::Combine($templateDir, "e2e.edge.and.plc.simulation.json")
$simulationDeployment = New-AzResourceGroupDeployment -ResourceGroupName $resourceGroupName -TemplateFile $template -TemplateParameterObject $templateParameters
if ($simulationDeployment.ProvisioningState -ne "Succeeded") {
    Write-Error "Deployment $($simulationDeployment.ProvisioningState)." -ErrorAction Stop
}

Write-Host "Deployed simulation"
