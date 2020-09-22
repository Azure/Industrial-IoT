Param(
    $KeyVaultName,
    $ResourceGroupName,
    $ObjectId
)

Write-Host "Adding List,Get-Permissions for secrets of vault '$($KeyVaultName)' for user '$($upn)'"
Set-AzKeyVaultAccessPolicy -VaultName $KeyVaultName -ResourceGroupName $ResourceGroupName -ObjectId $objectId -PermissionsToSecrets get,list