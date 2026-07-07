Param(
    [string] $ResourceGroupName,
    [Guid] $TenantId,
    [string] $OpcPlcSimulationUrls,
    [string] $OpcPlcSimulationIps,
    [string] $EdgeIdentity,
    [string] $EdgeVmUsername,
    [string] $Fqdn,
    [string] $SshPrivateKey,
    [string] $SshPublicKey
)

# Stop execution when an error occurs.
$ErrorActionPreference = "Stop"

if (!$ResourceGroupName) {
    Write-Error "ResourceGroupName not set."
}

## Check if KeyVault exists
$keyVault = "e2etestingkeyVault" + $testSuffix
$keyVaultList = az keyvault list --resource-group $ResourceGroupName | ConvertFrom-Json
if ($keyVaultList.Count -ne 1){
    Write-Error "keyVault could not be automatically selected in Resource Group '$($ResourceGroupName)'."
}

$keyVault = $keyVaultList.name
Write-Host "The following Key Vault has been selected: $keyVault"

Write-Host "Adding/Updating KeyVault-Secret 'plc-simulation-urls' with value '$($OpcPlcSimulationUrls)'..."
az keyvault secret set --vault-name $keyVault --name "plc-simulation-urls" --value $OpcPlcSimulationUrls > $null
Write-Host "Adding/Updating KeyVault-Secret 'plc-simulation-ips' with value '$($OpcPlcSimulationIps)'..."
az keyvault secret set --vault-name $keyVault --name "plc-simulation-ips" --value $OpcPlcSimulationIps > $null

Write-Host "Adding/Updating KeyVault-Secret 'iot-edge-device-id' with value '$($EdgeIdentity)'..."
az keyvault secret set --vault-name $keyVault --name 'iot-edge-device-id' --value $EdgeIdentity > $null
Write-Host "Adding/Updating KeVault-Secret 'iot-edge-vm-username' with value '$($EdgeVmUsername)'..."
az keyvault secret set --vault-name $keyVault --name 'iot-edge-vm-username' --value $EdgeVmUsername > $null
Write-Host "Adding/Updating KeyVault-Secret 'iot-edge-device-dnsname' with value '$($Fqdn)'..."
az keyvault secret set --vault-name $keyVault --name 'iot-edge-device-dnsname' --value $Fqdn > $null

# Persist the SSH key pair in the per-run Key Vault so the reusable test
# workflow (e2e-run-tests.yml) can read them and inject them into the test
# process. The Key Vault is created per run and deleted during cleanup, so the
# throwaway VM key never persists beyond the run.
#
# The private key is stored base64-encoded (single line). A raw multi-line PEM
# does not survive the Key Vault -> az tsv -> $GITHUB_ENV -> environment
# variable transport intact -- it corrupts adjacent environment variables and
# leaks unmasked in logs. TestHelper.GetPrivateSshKey base64-decodes it.
if ($SshPrivateKey) {
    $privateKeyB64 = [Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($SshPrivateKey))
    Write-Host "Adding/Updating KeyVault-Secret 'iot-edge-vm-privatekey' (base64, value hidden)..."
    az keyvault secret set --vault-name $keyVault --name 'iot-edge-vm-privatekey' --value $privateKeyB64 > $null
}
else {
    Write-Warning "SshPrivateKey not provided; 'iot-edge-vm-privatekey' will not be set."
}

if ($SshPublicKey) {
    Write-Host "Adding/Updating KeyVault-Secret 'iot-edge-vm-publickey'..."
    az keyvault secret set --vault-name $keyVault --name 'iot-edge-vm-publickey' --value $SshPublicKey > $null
}
else {
    Write-Warning "SshPublicKey not provided; 'iot-edge-vm-publickey' will not be set."
}

Write-Host "Deployment finished."
