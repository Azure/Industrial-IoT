Param(
    [string]
    $ResourceGroupName,
    [string]
    $ServicePrincipalName
)

# Stop execution when an error occurs.
$ErrorActionPreference = "Stop"

if (!$ResourceGroupName) {
    Write-Error "ResourceGroupName not set."
}

$keyVaults = Get-AzKeyVault -ResourceGroupName $ResourceGroupName

if (!$keyVaults) {
    Write-Error "Could not find any KeyVaults in Resource Group ($ResourceGroupName)"
}

if ($ServicePrincipalName) {
    $keyVaults | %{
        Write-Host "Adding List,Get,Set-Permissions for secrets of vault '$($_.VaultName)' for ServicePrincipalName '$($ServicePrincipalName)'"
        Set-AzKeyVaultAccessPolicy -VaultName $_.VaultName -ResourceGroupName $ResourceGroupName -ServicePrincipalName $ServicePrincipalName -PermissionsToSecrets get,list,set 
    }
} else {
    if ($azContext.Account.Id) {
        $keyVaults | %{
            Write-Host "Adding List,Get,Set-Permissions for secrets of vault '$($_.VaultName)' for UserPrincipalName '$($azContext.Account.Id)'"
            Set-AzKeyVaultAccessPolicy -VaultName $_.VaultName -ResourceGroupName $ResourceGroupName -UserPrincipalName $azContext.Account.Id -PermissionsToSecrets get,list,set
        }
    } else {
        Write-Error "Not logged in" -ErrorAction Stop
    }
} 
