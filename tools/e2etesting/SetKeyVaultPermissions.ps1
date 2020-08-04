Param(
    $KeyVaultUrl,
    $ResourceGroupName,
    $ObjectId
)

$keyVaultName = $KeyVaultUrl.Split('.')[0].Split('/')[2]

Write-Host "Adding List,Get-Permissions for secrets of vault '$($keyVaultName)' for user '$($upn)'"
Set-AzKeyVaultAccessPolicy -VaultName $keyVaultName -ResourceGroupName $ResourceGroupName -ObjectId $objectId -PermissionsToSecrets get,list