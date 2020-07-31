Param(
    $KeyVaultUrl,
    $ResourceGroupName,
    $ObjectId
)

$keyVaultName = $KeyVaultUrl.Split('.')[0].Split('/')[2]

Write-Host "Adding List,Get-Permissions for secrets of vault '$($keyVaultName)' for user '$($upn)'"
Set-AzKeyVaultAccessPolicy -VaultName $keyVaultName -ResourceGroupName $ResourceGroupName -ObjectId $objectId -PermissionsToSecrets get,list

Write-Host "Setting output-variable 'KeyVaultName' to '$($keyVaultName)'"
Write-Host "##vso[task.setvariable variable=KeyVaultNameFromDeployment;isOutput=true]$($keyVaultName)"