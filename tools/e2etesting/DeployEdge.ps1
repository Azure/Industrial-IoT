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
    $KeysPath,
    [ValidateSet("1.4", "1.5")]
    [string]
    $EdgeTemplateVersion = "1.4"
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

Write-Host "Updating 'os' and '__type__'-Tags in Device Twin..."
Update-AzIotHubDeviceTwin -ResourceGroupName $ResourceGroupName -IotHubName $iotHub.Name -DeviceId $edgeIdentity.Id -Tag @{ "os" = "Linux"; "__type__" = "iiotedge"; } | Out-Null

## Generate SSH keys
## The keys are generated here, emitted to pipeline variables (marked as secret)
## and staged in the per-run Key Vault by SetTestVariables.ps1; the on-disk files
## are deleted immediately. The key is passphrase-less because the E2E tests load
## it with SSH.NET's PrivateKeyFile(stream) constructor, which cannot decrypt an
## encrypted key. The args are splatted so the empty -N value is passed reliably.
$privateKeyFilePath = Join-Path $KeysPath "id_rsa_iotedge"
$publicKeyFilePath = $privateKeyFilePath + ".pub"
$sshKeygenArgs = @('-q', '-m', 'PEM', '-b', '4096', '-t', 'rsa', '-f', $privateKeyFilePath, '-N', '')
Write-Output "y" | & ssh-keygen @sshKeygenArgs
$sshPrivateKey = Get-Content $privateKeyFilePath -Raw
$sshPublicKey = Get-Content $publicKeyFilePath -Raw

## Delete SSH keys from file system
Remove-Item -Path $privateKeyFilePath | Out-Null
Remove-Item -Path $publicKeyFilePath | Out-Null

## Deploy Edge VM
## NOTE: $edgeDeviceConnectionString is consumed only in-memory by the ARM template's
## cloud-init parameter below. It is intentionally never written to Key Vault or emitted
## as a pipeline variable. The IoT Edge device fundamentally needs a symmetric key (or
## X.509 cert) to authenticate to IoT Hub — that's inherent to IoT Edge and unavoidable.
## We minimize blast radius by generating the connection string just-in-time per
## deployment and never persisting it. The pipeline variable below is the device *Id*
## only (not the connection string), which is not sensitive.
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

# Use a vendored copy of the Azure/iotedge-vm-deploy template, selected by
# $EdgeTemplateVersion. Each edgeDeploy.<version>.json is identical to
# https://raw.githubusercontent.com/Azure/iotedge-vm-deploy/<version>/edgeDeploy.json
# except the VNet subnet sets "defaultOutboundAccess": false, which the
# "Deny Virtual Network Subnet Default Outbound Access" Azure Policy requires.
# The VM keeps outbound connectivity through its instance-level public IP.
# We use 1.4 today; 1.5 (Ubuntu 22.04 / IoT Edge 1.5 LTS) is vendored and ready
# to switch to by setting -EdgeTemplateVersion 1.5.
$edgeTemplateFile = Join-Path $PSScriptRoot "edgeDeploy.$($EdgeTemplateVersion).json"

Write-Host "Running IoT Edge VM Deployment (template version $($EdgeTemplateVersion))..."

try {
    $edgeDeployment = New-AzResourceGroupDeployment -ResourceGroupName $ResourceGroupName -TemplateFile $edgeTemplateFile -TemplateParameterObject $edgeParameters -ErrorAction Stop
}
catch {
    # New-AzResourceGroupDeployment surfaces only the opaque top-level
    # 'InvalidTemplateDeployment' summary on a preflight failure. Re-run the
    # validation to print the inner errors (e.g. SkuNotAvailable / capacity,
    # vCPU quota, Azure Policy) so the real cause is visible in the CI log.
    Write-Warning "Edge VM deployment failed: $($_.Exception.Message)"
    Write-Host "Surfacing inner validation errors via Test-AzResourceGroupDeployment..."
    $validationErrors = Test-AzResourceGroupDeployment -ResourceGroupName $ResourceGroupName -TemplateFile $edgeTemplateFile -TemplateParameterObject $edgeParameters -ErrorAction SilentlyContinue
    foreach ($validationError in $validationErrors) {
        Write-Host "Validation error: Code=$($validationError.Code); Message=$($validationError.Message)"
        foreach ($detail in $validationError.Details) {
            Write-Host "  Detail: Code=$($detail.Code); Message=$($detail.Message)"
            foreach ($innerDetail in $detail.Details) {
                Write-Host "    Inner: Code=$($innerDetail.Code); Message=$($innerDetail.Message)"
            }
        }
    }
    throw
}

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

# Emit CI variables via the shared helper. This writes the ADO
# ##vso[task.setvariable ...] command AND the GitHub Actions
# $GITHUB_OUTPUT / $GITHUB_ENV entries. SshPrivateKey is marked -Secret so
# it is masked from logs on both systems (the helper registers the GH mask
# BEFORE the ADO command line is written to stdout, which is the only place
# the raw value would otherwise appear in GH logs).
. (Join-Path $PSScriptRoot '_ci.ps1')
Set-CIVariable -Name 'EdgeIdentity'   -Value $edgeIdentity.Id
Set-CIVariable -Name 'EdgeVmUsername' -Value $edgeVmUsername
Set-CIVariable -Name 'SshPrivateKey'  -Value $sshPrivateKey -Secret
Set-CIVariable -Name 'SshPublicKey'   -Value $sshPublicKey -Secret
Set-CIVariable -Name 'Fqdn'           -Value $fqdn

Write-Host "Deployment finished."