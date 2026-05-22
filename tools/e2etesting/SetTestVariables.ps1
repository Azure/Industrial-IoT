Param(
    [string] $ResourceGroupName,
    [Guid] $TenantId,
    [string] $OpcPlcSimulationUrls,
    [string] $OpcPlcSimulationIps,
    [string] $EdgeIdentity,
    [string] $EdgeVmUsername,
    [string] $Fqdn
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

# NOTE: SshPrivateKey / SshPublicKey are intentionally NOT stored in Key Vault.
# They are pipeline-secret variables produced by DeployEdge.ps1 (with issecret=true)
# and consumed directly by runtests.yml as IOT_EDGE_VM_PRIVATEKEY env var on the
# test process. The key never persists at rest in our infra (Phase 1.8 Option A).

Write-Host "Deployment finished."
