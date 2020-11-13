Param(
    [string]
    $keyVaultName,
    [string]
    $resourceGroupName,
    [string]
    $branchName,
    [string]
    $region,
    [string]
    $client_id,
    [string]
    $client_secret,
    [string]
    $tenantId
)

az config set extension.use_dynamic_install=yes_without_prompt

# Powershell-Az module (which is automatically authorized by Powershell) 
# don't support IoT Hub Device Identity methods, we ned to use az cli
if(-Not ([string]::IsNullOrEmpty($client_id) -or [string]::IsNullOrEmpty($client_secret) -or [string]::IsNullOrEmpty($tenantId))) {
    az login --service-principal --username  $client_id --password $client_secret --tenant $tenantId
} else {
    Write-Warning "username or password or tenantId for az login are not provided - skipped"
}

$branchName = $branchName.Replace('refs/heads/', '')
$templateDir = [System.IO.Path]::Combine($PSScriptRoot, "../../deploy/templates") 

# Get IoT Hub
$iotHub = Get-AzIotHub -ResourceGroupName $resourceGroupName

# Allow the keyvault to be used in ARM deployments
Set-AzKeyVaultAccessPolicy -VaultName $keyVaultName -EnabledForTemplateDeployment
Write-Host "Key vault configured to be used in ARM deployments"

# Register IoT Edge device at IoT Hub
$testEdgeDevice = "myTestDevice_$(Get-Random)"
$edgeIdentity = az iot hub device-identity create --device-id $testEdgeDevice --edge-enabled --hub-name $iotHub.Name | ConvertFrom-Json
Write-Host "Created edge device identity $($edgeIdentity.deviceId)"

# Store Device Id within KeyVault to be useable by rerun without deployment (USeExisting=true)
$secretvalue = ConvertTo-SecureString $edgeIdentity.deviceId -AsPlainText -Force
$secret = Set-AzKeyVaultSecret -VaultName $keyVaultName -Name 'iot-edge-device-id' -SecretValue $secretvalue
if (-Not $secret.Enabled) {
    Write-Error "Couldn't store device identity in KeyVault" -ErrorAction Stop
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
$out = az iot hub device-twin replace --device-id $edgeIdentity.deviceId --hub-name $iotHub.Name --json $configFileName
Write-Host "Added Industrial IoT tags to Device Twin of edge device"

$vmpassword = -join ((65..90) + (97..122) + (33..38) + (40..47) + (48..57)| Get-Random -Count 20 | % {[char]$_})
$numberOfSimulations = 18
# Deploy edge and simulation virtual machines
$templateParameters = @{
    "factoryName" = "contoso"
    "vmUsername" = "sandboxuser"
    "vmPassword" = $vmpassword
    "edgeVmSize" = "Standard_D2s_v3"
    "numberOfSimulations" = $numberOfSimulations
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

# Execute deployment using ARM template
$template = [System.IO.Path]::Combine($templateDir, "e2e.edge.and.plc.simulation.json")
$simulationDeployment = New-AzResourceGroupDeployment -ResourceGroupName $resourceGroupName -TemplateFile $template -TemplateParameterObject $templateParameters
if ($simulationDeployment.ProvisioningState -ne "Succeeded") {
    Write-Error "Deployment $($simulationDeployment.ProvisioningState)." -ErrorAction Stop
}

# Generate URLs to access published_nodes.json via HTTP endpoint of plc-simulation.
$plcSimNames = ""
for ($i=1; $i -le $numberOfSimulations; $i++) {
    $aci = az container show --resource-group $resourceGroupName --name "$($simulationDeployment.Outputs.plcPrefix.Value)$($i)" | ConvertFrom-Json
    $plcSimNames += "http://$($aci.ipAddress.fqdn);"
}

# Store Url to PLC simulations in KeyVault
$secretvalue = ConvertTo-SecureString $plcSimNames -AsPlainText -Force
$secret = Set-AzKeyVaultSecret -VaultName $keyVaultName -Name 'plc-simulation-urls' -SecretValue $secretvalue
if (-Not $secret.Enabled) {
    Write-Error "Couldn't store URLs to access PLC simulation in KeyVault" -ErrorAction Stop
}
Write-Host "Stored URLs to access PLC simulation in KeyVault"

# Store DNS to IoT Edge device in KeyVault
$dns = az network public-ip show --name $simulationDeployment.Outputs.publicIP.Value --resource-group $resourceGroupName | ConvertFrom-Json
$secretvalue = ConvertTo-SecureString $dns.dnsSettings.fqdn -AsPlainText -Force
$secret = Set-AzKeyVaultSecret -VaultName $keyVaultName -Name 'iot-edge-device-dns-name' -SecretValue $secretvalue
if (-Not $secret.Enabled) {
    Write-Error "Couldn't store DNS of IoT Edge device in KeyVault" -ErrorAction Stop
}
Write-Host "Stored DNS to access IoT Edge device in KeyVault"

Write-Host "Deployed simulation"
