Param(
    $keyVaultName,
    $resourceGroupName,
    $branchName
)

$branchName = $branchName.Replace('refs/heads/', '')
$templateDir = [System.IO.Path]::Combine($PSScriptRoot, "../../deploy/templates") 

# Get IoT Hub
$iotHub = Get-AzIotHub -ResourceGroupName $resourceGroupName
$ioTHubConnString = (Get-AzIotHubConnectionString -ResourceGroupName $resourceGroupName -KeyName iothubowner -Name $iotHub.Name).PrimaryConnectionString

# Create DPS
Write-Host "Creating device provisioning service"

$templateParameters = @{
    "dpsIotHubHostName" = $iotHub.Properties.HostName
    "dpsIotHubConnectionString" = $ioTHubConnString
    "dpsIotHubLocation" = $iotHub.Location
    "keyVaultName" = $keyVaultName
    "branchName" = $branchName
}

$template = [System.IO.Path]::Combine($templateDir, "azuredeploy.deviceprovisioning.json")
$dpsDeployment = New-AzResourceGroupDeployment -ResourceGroupName $resourceGroupName -TemplateFile $template -TemplateParameterObject $templateParameters
if ($dpsDeployment.ProvisioningState -ne "Succeeded") {
    Write-Error "Deployment $($dpsDeployment.ProvisioningState)." -ErrorAction Stop
}

Write-Host "Created DPS"

# Create MSI for edge
Write-Host "Creating MSI for edge VM identity"

$template = [System.IO.Path]::Combine($templateDir, "azuredeploy.managedidentity.json")
$prereqsDeployment = New-AzResourceGroupDeployment -ResourceGroupName $resourceGroupName -TemplateFile $template
if ($prereqsDeployment.ProvisioningState -ne "Succeeded") {
    Write-Error "Deployment $($prereqsDeployment.ProvisioningState)." -ErrorAction Stop
}

Write-Host "Created MSI $($prereqsDeployment.Parameters.managedIdentityName.Value) with resource id $($prereqsDeployment.Outputs.managedIdentityResourceId.Value)"

# Configure the keyvault
# Allow the MSI to access keyvault
# https://github.com/Azure/azure-powershell/issues/10029 for why -BypassObjectIdValidation is needed
Set-AzKeyVaultAccessPolicy -VaultName $keyVaultName -ObjectId $prereqsDeployment.Outputs.managedIdentityPrincipalId.Value -PermissionsToSecrets get,list,set,delete -PermissionsToKeys get,list,sign,unwrapKey,wrapKey,create -PermissionsToCertificates get,list,update,create,import -BypassObjectIdValidation
Write-Host "Key vault set to allow MSI full access"

# Allow the keyvault to be used in ARM deployments
Set-AzKeyVaultAccessPolicy -VaultName $keyVaultName -EnabledForTemplateDeployment
Write-Host "Key vault configured to be used in ARM deployments"

# Deploy edge and simulation virtual machines
$templateParameters = @{
    "keyVaultName" = $keyVaultName
	"managedIdentityResourceId" = $prereqsDeployment.Outputs.managedIdentityResourceId.Value
    "numberOfLinuxGateways" = 1
    "edgePassword" = [System.Web.Security.Membership]::GeneratePassword(15, 5)
    "branchName" = $branchName
}

Write-Host "Preparing to deploy azuredeploy.simulation.json"

$template = [System.IO.Path]::Combine($templateDir, "azuredeploy.simulation.json")
$simulationDeployment = New-AzResourceGroupDeployment -ResourceGroupName $resourceGroupName -TemplateFile $template -TemplateParameterObject $templateParameters
if ($simulationDeployment.ProvisioningState -ne "Succeeded") {
    Write-Error "Deployment $($simulationDeployment.ProvisioningState)." -ErrorAction Stop
}

Write-Host "Deployed simulation"