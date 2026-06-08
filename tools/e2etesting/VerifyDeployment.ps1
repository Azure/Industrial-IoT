Param(
    [Parameter(Mandatory = $true)]
    [string] $ResourceGroupName,
    # Optional: when not specified, the script discovers the singletons in the RG.
    [string] $KeyVaultName,
    [string] $IoTHubName,
    [string] $AcrName
)

# Stop execution when an error occurs.
$ErrorActionPreference = "Stop"

# Post-deployment verification for the e2e test resource group. Fails loudly if any
# resource still permits local-auth (shared keys, access policies, admin user).
# Intended to run after main.bicep deploys; also useful when validating a hand-deployed
# environment.

Write-Host "Verifying no-local-auth state in resource group $ResourceGroupName..."

function Assert-True {
    Param([Parameter(Mandatory = $true)][string]$Description, [Parameter(Mandatory = $true)][bool]$Condition)
    if ($Condition) {
        Write-Host "  PASS: $Description"
    } else {
        Write-Error "  FAIL: $Description"
    }
}

# --- Key Vault: must use RBAC, not access policies (Phase 1.1) ---
if (!$KeyVaultName) {
    $kvs = Get-AzKeyVault -ResourceGroupName $ResourceGroupName
    if ($kvs.Count -ne 1) {
        Write-Error "Expected exactly 1 Key Vault in RG '$ResourceGroupName'; found $($kvs.Count). Pass -KeyVaultName explicitly."
    }
    $KeyVaultName = $kvs[0].VaultName
}
$kv = Get-AzKeyVault -ResourceGroupName $ResourceGroupName -VaultName $KeyVaultName
Assert-True -Description "Key Vault '$KeyVaultName' uses RBAC (enableRbacAuthorization=true)" `
    -Condition ($kv.EnableRbacAuthorization -eq $true)

# --- IoT Hub: data plane local auth must be disabled (Phase 1.3 + 1.4) ---
if (!$IoTHubName) {
    $iotHubs = Get-AzIotHub -ResourceGroupName $ResourceGroupName
    if ($iotHubs.Count -ne 1) {
        Write-Error "Expected exactly 1 IoT Hub in RG '$ResourceGroupName'; found $($iotHubs.Count). Pass -IoTHubName explicitly."
    }
    $IoTHubName = $iotHubs[0].Name
}
$iothub = Get-AzIotHub -ResourceGroupName $ResourceGroupName -Name $IoTHubName
Assert-True -Description "IoT Hub '$IoTHubName' has disableLocalAuth=true" `
    -Condition ($iothub.Properties.DisableLocalAuth -eq $true)

# --- ACR: admin user must be disabled (Phase 1.5) ---
if (!$AcrName) {
    $acrs = Get-AzContainerRegistry -ResourceGroupName $ResourceGroupName
    if ($acrs.Count -eq 0) {
        Write-Host "  SKIP: No ACR found in RG '$ResourceGroupName' (verifier ACR may not be deployed)."
        $AcrName = $null
    } elseif ($acrs.Count -gt 1) {
        Write-Error "Multiple ACRs in RG '$ResourceGroupName'; pass -AcrName explicitly."
    } else {
        $AcrName = $acrs[0].Name
    }
}
if ($AcrName) {
    $acr = Get-AzContainerRegistry -ResourceGroupName $ResourceGroupName -Name $AcrName
    Assert-True -Description "ACR '$AcrName' has adminUserEnabled=false" `
        -Condition ($acr.AdminUserEnabled -eq $false)
}

# --- Storage: must not exist in the test RG (Phase 1.2) ---
$storageAccounts = Get-AzStorageAccount -ResourceGroupName $ResourceGroupName -ErrorAction SilentlyContinue
Assert-True -Description "No storage account in test RG (Phase 1.2: storage removed)" `
    -Condition ($null -eq $storageAccounts -or $storageAccounts.Count -eq 0)

Write-Host ""
Write-Host "Verification complete."
