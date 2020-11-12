Param(
    [string]
    $KeyVaultName,
    [string]
    $ResourceGroupName,
    [string]
    $ServicePrincipalName
)

$azContext = Get-AzContext
Write-Host "AccountId: $($azContext.Account.Id)"
Write-Host "Context:"
$azContext

if ($ServicePrincipalName) {
    Write-Host "Adding List,Get-Permissions for secrets of vault '$($KeyVaultName)' for ServicePrincipalName '$($ServicePrincipalName)'"
    Set-AzKeyVaultAccessPolicy -VaultName $KeyVaultName -ResourceGroupName $ResourceGroupName -ServicePrincipalName $ServicePrincipalName -PermissionsToSecrets get,list,set
} else {
    if ($azContext.Account.Id) {
        Write-Host "Adding List,Get-Permissions for secrets of vault '$($KeyVaultName)' for UserPrincipalName '$($azContext.Account.Id)'"
        Set-AzKeyVaultAccessPolicy -VaultName $KeyVaultName -ResourceGroupName $ResourceGroupName -UserPrincipalName $azContext.Account.Id -PermissionsToSecrets get,list,set
    } else {
        Write-Error "Not logged in" -ErrorAction Stop
    }
} 